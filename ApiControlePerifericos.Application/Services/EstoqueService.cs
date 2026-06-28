using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using Microsoft.Extensions.Logging;

namespace ApiControlePerifericos.Services
{
    public class EstoqueService : IEstoqueService
    {
        private readonly IUnitOfWork _uof;
        private readonly ILogger<EstoqueService> _logger;

        public EstoqueService(IUnitOfWork uof, ILogger<EstoqueService> logger)
        {
            _uof = uof;
            _logger = logger;
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
            {
                _logger.LogWarning("Saída cancelada: colaborador {ColaboradorId} não encontrado.", colaboradorId);
                return EstoqueResult.Falha(EstoqueResultStatus.ColaboradorNaoEncontrado,
                    $"Colaborador com ID {colaboradorId} não encontrado.");
            }

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
            await _uof.CommitAsync();

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
    }
}
