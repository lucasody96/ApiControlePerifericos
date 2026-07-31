using ApiControlePerifericos.DTOs;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using Microsoft.Extensions.Logging;

namespace ApiControlePerifericos.Services
{
    public class EstoqueService : IEstoqueService
    {
        private readonly IUnitOfWork _uof;
        private readonly ILogger<EstoqueService> _logger;
        private readonly IProdutoCacheInvalidator _produtoCache;

        public EstoqueService(IUnitOfWork uof, ILogger<EstoqueService> logger, IProdutoCacheInvalidator produtoCache)
        {
            _uof = uof;
            _logger = logger;
            _produtoCache = produtoCache;
        }

        public async Task<EstoqueResult> RegistrarEntradaAsync(int produtoId, int quantidade, string? registradoPor)
        {
            var produto = await _uof.ProdutoRepository.GetByIdTrackedAsync(produtoId);
            if (produto is null)
                return ProdutoNaoEncontrado(produtoId);

            produto.SaldoAtual += quantidade;

            return await PersistirAsync('E', produtoId, quantidade, colaboradorId: null, registradoPor);
        }

        public async Task<EstoqueResult> RegistrarSaidaAsync(int produtoId, int quantidade, int colaboradorId, string? registradoPor)
        {
            var produto = await _uof.ProdutoRepository.GetByIdTrackedAsync(produtoId);
            if (produto is null)
                return ProdutoNaoEncontrado(produtoId);

            var colaborador = await _uof.ColaboradorRepository.GetAsync(c => c.ColaboradorId == colaboradorId);
            if (colaborador is null)
                return ColaboradorNaoEncontrado(colaboradorId);

            if (produto.SaldoAtual < quantidade)
                return SaldoInsuficiente(produto, quantidade);

            produto.SaldoAtual -= quantidade;

            return await PersistirAsync('S', produtoId, quantidade, colaboradorId, registradoPor);
        }

        public async Task<EstoqueResult> RegistrarAjusteAsync(int produtoId, int quantidade, string? registradoPor)
        {
            var produto = await _uof.ProdutoRepository.GetByIdTrackedAsync(produtoId);
            if (produto is null)
                return ProdutoNaoEncontrado(produtoId);

            if (produto.SaldoAtual < quantidade)
                return SaldoInsuficiente(produto, quantidade);

            produto.SaldoAtual -= quantidade;

            return await PersistirAsync('A', produtoId, quantidade, colaboradorId: null, registradoPor);
        }

        public async Task<EstoqueResult> AtualizarMovimentacaoAsync(int movimentacaoId, MovimentacaoDTO dto)
        {
            var movimentacao = await _uof.MovimentacaoRepository.GetByIdTrackedAsync(movimentacaoId);
            if (movimentacao is null)
                return MovimentacaoNaoEncontrada(movimentacaoId);

            if (!TipoEhValido(dto.Tipo))
                return TipoInvalido(dto.Tipo);

            var produtoOrigem = await _uof.ProdutoRepository.GetByIdTrackedAsync(movimentacao.ProdutoId);
            if (produtoOrigem is null)
                return ProdutoNaoEncontrado(movimentacao.ProdutoId);

            // O PUT pode trocar o produto da movimentação: nesse caso o estorno cai num
            // produto e o novo lançamento no outro. Quando é o mesmo, reaproveita a
            // instância já rastreada — buscar de novo traria o mesmo objeto, mas o
            // cálculo abaixo ficaria ambíguo sobre em qual saldo cada parcela incide.
            var mesmoProduto = dto.ProdutoId == movimentacao.ProdutoId;
            var produtoDestino = mesmoProduto
                ? produtoOrigem
                : await _uof.ProdutoRepository.GetByIdTrackedAsync(dto.ProdutoId);

            if (produtoDestino is null)
                return ProdutoNaoEncontrado(dto.ProdutoId);

            // Colaborador só existe na saída; nos outros tipos o vínculo é descartado.
            int? colaboradorId = null;
            if (dto.Tipo == 'S')
            {
                if (dto.ColaboradorId is null)
                {
                    _logger.LogWarning("Alteração cancelada: saída da movimentação {MovimentacaoId} sem colaborador.", movimentacaoId);
                    return EstoqueResult.Falha(EstoqueResultStatus.ColaboradorObrigatorio,
                        "Movimentação de saída exige um colaborador.");
                }

                var colaborador = await _uof.ColaboradorRepository.GetAsync(c => c.ColaboradorId == dto.ColaboradorId.Value);
                if (colaborador is null)
                    return ColaboradorNaoEncontrado(dto.ColaboradorId.Value);

                colaboradorId = dto.ColaboradorId;
            }

            var estorno = Delta(movimentacao.Tipo, movimentacao.Quantidade);
            var lancamento = Delta(dto.Tipo, dto.Quantidade);

            // Os saldos são projetados antes de qualquer atribuição: numa falha nada
            // pode ter sido alterado nas entidades rastreadas.
            if (mesmoProduto)
            {
                // Só o saldo FINAL importa. O estorno sozinho pode passar pelo negativo e
                // o lançamento novo trazer o saldo de volta — ex.: entrada de 10 (saldo 0
                // após estorno = -10) virando entrada de 20, que termina em +10.
                var saldo = produtoOrigem.SaldoAtual - estorno + lancamento;
                if (saldo < 0)
                    return SaldoNegativoAposEstorno(produtoOrigem, saldo);

                produtoOrigem.SaldoAtual = saldo;
            }
            else
            {
                var saldoOrigem = produtoOrigem.SaldoAtual - estorno;
                var saldoDestino = produtoDestino.SaldoAtual + lancamento;

                if (saldoOrigem < 0)
                    return SaldoNegativoAposEstorno(produtoOrigem, saldoOrigem);

                if (saldoDestino < 0)
                    return SaldoNegativoAposEstorno(produtoDestino, saldoDestino);

                produtoOrigem.SaldoAtual = saldoOrigem;
                produtoDestino.SaldoAtual = saldoDestino;
            }

            // A movimentação está rastreada: alterar as propriedades basta, sem passar
            // pelo Update do repositório (que anexaria uma segunda instância da mesma PK).
            movimentacao.Tipo = dto.Tipo;
            movimentacao.Quantidade = dto.Quantidade;
            movimentacao.DataMovimentacao = dto.DataMovimentacao;
            movimentacao.RegistradoPor = dto.RegistradoPor;
            movimentacao.ProdutoId = dto.ProdutoId;
            movimentacao.ColaboradorId = colaboradorId;

            await ConfirmarEInvalidarCacheAsync();

            _logger.LogInformation("Movimentação {MovimentacaoId} alterada para tipo {Tipo}, quantidade {Quantidade}, produto {ProdutoId}.",
                movimentacaoId, dto.Tipo, dto.Quantidade, dto.ProdutoId);

            return EstoqueResult.Ok(movimentacao);
        }

        public async Task<EstoqueResult> ExcluirMovimentacaoAsync(int movimentacaoId)
        {
            var movimentacao = await _uof.MovimentacaoRepository.GetByIdTrackedAsync(movimentacaoId);
            if (movimentacao is null)
                return MovimentacaoNaoEncontrada(movimentacaoId);

            var produto = await _uof.ProdutoRepository.GetByIdTrackedAsync(movimentacao.ProdutoId);
            if (produto is null)
                return ProdutoNaoEncontrado(movimentacao.ProdutoId);

            // Excluir é estornar: desfaz no saldo o efeito que a movimentação teve.
            var saldo = produto.SaldoAtual - Delta(movimentacao.Tipo, movimentacao.Quantidade);
            if (saldo < 0)
                return SaldoNegativoAposEstorno(produto, saldo);

            produto.SaldoAtual = saldo;
            _uof.MovimentacaoRepository.Delete(movimentacao);

            await ConfirmarEInvalidarCacheAsync();

            _logger.LogInformation("Movimentação {MovimentacaoId} excluída: saldo do produto {ProdutoId} estornado para {Saldo}.",
                movimentacaoId, produto.ProdutoId, saldo);

            return EstoqueResult.Ok(movimentacao);
        }

        // Efeito de uma movimentação sobre o SaldoAtual: entrada soma, saída e ajuste subtraem.
        private static int Delta(char tipo, int quantidade) =>
            tipo == 'E' ? quantidade : -quantidade;

        private static bool TipoEhValido(char tipo) =>
            tipo is 'E' or 'S' or 'A';

        // Confirma tudo o que está rastreado (saldo dos produtos + movimentação) num
        // único CommitAsync e só então invalida o cache, já com a alteração persistida.
        private async Task ConfirmarEInvalidarCacheAsync()
        {
            await _uof.CommitAsync();
            _produtoCache.InvalidarProdutos();
        }

        // Cria a movimentação e confirma tudo (saldo do produto rastreado + movimentação)
        // num único CommitAsync — o EF Core envolve em uma transação automaticamente.
        private async Task<EstoqueResult> PersistirAsync(char tipo, int produtoId, int quantidade,
                                                         int? colaboradorId, string? registradoPor)
        {
            var movimentacao = new Movimentacao
            {
                Tipo = tipo,
                Quantidade = quantidade,
                DataMovimentacao = DateTime.Now,
                RegistradoPor = registradoPor,
                ProdutoId = produtoId,
                ColaboradorId = colaboradorId
            };

            _uof.MovimentacaoRepository.Create(movimentacao);

            // A movimentação alterou o SaldoAtual do produto (entidade rastreada), sem
            // passar pelo Update do repositório — daí a invalidação explícita do cache.
            await ConfirmarEInvalidarCacheAsync();

            _logger.LogInformation("Movimentação tipo {Tipo} registrada: produto {ProdutoId}, quantidade {Quantidade}.",
                tipo, produtoId, quantidade);

            return EstoqueResult.Ok(movimentacao);
        }

        private EstoqueResult ProdutoNaoEncontrado(int produtoId)
        {
            _logger.LogWarning("Movimentação cancelada: produto {ProdutoId} não encontrado.", produtoId);
            return EstoqueResult.Falha(EstoqueResultStatus.ProdutoNaoEncontrado,
                $"Produto com ID {produtoId} não encontrado.");
        }

        private EstoqueResult SaldoInsuficiente(Produto produto, int quantidade)
        {
            _logger.LogWarning("Movimentação cancelada: saldo insuficiente do produto {ProdutoId} (saldo {Saldo}, solicitado {Quantidade}).",
                produto.ProdutoId, produto.SaldoAtual, quantidade);
            return EstoqueResult.Falha(EstoqueResultStatus.SaldoInsuficiente,
                $"Saldo insuficiente para o produto '{produto.Descricao}'. Saldo atual: {produto.SaldoAtual}, solicitado: {quantidade}.");
        }

        private EstoqueResult ColaboradorNaoEncontrado(int colaboradorId)
        {
            _logger.LogWarning("Movimentação cancelada: colaborador {ColaboradorId} não encontrado.", colaboradorId);
            return EstoqueResult.Falha(EstoqueResultStatus.ColaboradorNaoEncontrado,
                $"Colaborador com ID {colaboradorId} não encontrado.");
        }

        private EstoqueResult MovimentacaoNaoEncontrada(int movimentacaoId)
        {
            _logger.LogWarning("Operação cancelada: movimentação {MovimentacaoId} não encontrada.", movimentacaoId);
            return EstoqueResult.Falha(EstoqueResultStatus.MovimentacaoNaoEncontrada,
                $"Movimentação com ID {movimentacaoId} não encontrada.");
        }

        private EstoqueResult TipoInvalido(char tipo)
        {
            _logger.LogWarning("Alteração cancelada: tipo de movimentação '{Tipo}' inválido.", tipo);
            return EstoqueResult.Falha(EstoqueResultStatus.TipoInvalido,
                $"Tipo de movimentação '{tipo}' inválido. Use 'E' (entrada), 'S' (saída) ou 'A' (ajuste).");
        }

        private EstoqueResult SaldoNegativoAposEstorno(Produto produto, int saldoProjetado)
        {
            _logger.LogWarning("Operação cancelada: estorno deixaria o produto {ProdutoId} com saldo {SaldoProjetado}.",
                produto.ProdutoId, saldoProjetado);
            return EstoqueResult.Falha(EstoqueResultStatus.SaldoNegativoAposEstorno,
                $"A operação deixaria o produto '{produto.Descricao}' com saldo negativo ({saldoProjetado}).");
        }
    }
}
