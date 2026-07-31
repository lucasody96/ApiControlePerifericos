using ApiControlePerifericos.DTOs;
using ApiControlePerifericos.DTOs.Estoque;
using ApiControlePerifericos.Extensions;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using ApiControlePerifericos.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;

namespace ApiControlePerifericos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MovimentacoesController : ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly ILogger<MovimentacoesController> _logger;
        private readonly IMapper _mapper;
        private readonly IEstoqueService _estoqueService;

        public MovimentacoesController(IUnitOfWork uof, ILogger<MovimentacoesController> logger,
                                       IMapper mapper, IEstoqueService estoqueService)
        {
            _uof = uof;
            _logger = logger;
            _mapper = mapper;
            _estoqueService = estoqueService;
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<IEnumerable<MovimentacaoDTO>>> Get()
        {
            var movimentacoes = await _uof.MovimentacaoRepository.GetAllAsync();
            if (movimentacoes is null || !movimentacoes.Any())
            {
                _logger.LogInformation("Nenhuma movimentação encontrada.");
                return NotFound("Nenhuma movimentação encontrada.");
            }

            var movimentacoesDTO = _mapper.Map<IEnumerable<MovimentacaoDTO>>(movimentacoes);
            return Ok(movimentacoesDTO);
        }

        [HttpGet("{id}", Name = "ObterMovimentacao")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<MovimentacaoDTO>> Get(int id)
        {
            var movimentacao = await _uof.MovimentacaoRepository.GetAsync(m => m.MovimentacaoId == id);
            if (movimentacao is null)
            {
                _logger.LogWarning("Movimentação com ID {Id} não encontrada.", id);
                return NotFound($"Movimentação com ID {id} não encontrada.");
            }

            var movimentacaoDTO = _mapper.Map<MovimentacaoDTO>(movimentacao);
            return Ok(movimentacaoDTO);
        }

        [HttpGet("pagination")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<IEnumerable<MovimentacaoDTO>>> Get([FromQuery] MovimentacoesParameters parameters)
        {
            var intervaloInvalido = ValidarIntervaloDeDatas(parameters);
            if (intervaloInvalido is not null)
                return intervaloInvalido;

            var movimentacoes = await _uof.MovimentacaoRepository.GetMovimentacoesAsync(parameters);
            return ObterMovimentacoes(movimentacoes);
        }

        [HttpGet("relatorio")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<IEnumerable<MovimentacaoRelatorioDTO>>> GetRelatorio([FromQuery] MovimentacoesParameters parameters)
        {
            var intervaloInvalido = ValidarIntervaloDeDatas(parameters);
            if (intervaloInvalido is not null)
                return intervaloInvalido;

            var movimentacoes = await _uof.MovimentacaoRepository.GetRelatorioAsync(parameters);

            if (movimentacoes is null || !movimentacoes.Any())
            {
                _logger.LogInformation("Nenhuma movimentação encontrada para o relatório.");
                return NotFound("Nenhuma movimentação encontrada para o relatório.");
            }

            Response.AdicionarHeaderDePaginacao(movimentacoes);

            var relatorioDTO = _mapper.Map<IEnumerable<MovimentacaoRelatorioDTO>>(movimentacoes);

            return Ok(relatorioDTO);
        }

        [HttpGet("produto/{produtoId:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<IEnumerable<MovimentacaoDTO>>> GetByProduto(int produtoId)
        {
            var movimentacoes = await _uof.MovimentacaoRepository.GetByProdutoIdAsync(produtoId);
            if (movimentacoes is null || !movimentacoes.Any())
            {
                _logger.LogInformation("Nenhuma movimentação encontrada para o produto {ProdutoId}.", produtoId);
                return NotFound($"Nenhuma movimentação encontrada para o produto {produtoId}.");
            }

            var movimentacoesDTO = _mapper.Map<IEnumerable<MovimentacaoDTO>>(movimentacoes);
            return Ok(movimentacoesDTO);
        }

        [HttpGet("colaborador/{colaboradorId:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<IEnumerable<MovimentacaoDTO>>> GetByColaborador(int colaboradorId)
        {
            var movimentacoes = await _uof.MovimentacaoRepository.GetByColaboradorIdAsync(colaboradorId);
            if (movimentacoes is null || !movimentacoes.Any())
            {
                _logger.LogInformation("Nenhuma movimentação encontrada para o colaborador {ColaboradorId}.", colaboradorId);
                return NotFound($"Nenhuma movimentação encontrada para o colaborador {colaboradorId}.");
            }

            var movimentacoesDTO = _mapper.Map<IEnumerable<MovimentacaoDTO>>(movimentacoes);
            return Ok(movimentacoesDTO);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("entrada")]
        public async Task<ActionResult<MovimentacaoDTO>> Entrada(EntradaEstoqueRequest request)
        {
            var resultado = await _estoqueService.RegistrarEntradaAsync(
                request.ProdutoId, request.Quantidade, User.Identity?.Name);

            return ProcessarResultado(resultado);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("saida")]
        public async Task<ActionResult<MovimentacaoDTO>> Saida(SaidaEstoqueRequest request)
        {
            var resultado = await _estoqueService.RegistrarSaidaAsync(
                request.ProdutoId, request.Quantidade, request.ColaboradorId, User.Identity?.Name);

            return ProcessarResultado(resultado);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("ajuste")]
        public async Task<ActionResult<MovimentacaoDTO>> Ajuste(AjusteEstoqueRequest request)
        {
            var resultado = await _estoqueService.RegistrarAjusteAsync(
                request.ProdutoId, request.Quantidade, User.Identity?.Name);

            return ProcessarResultado(resultado);
        }

        [Authorize(Policy = "SuperAdminOnly")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<MovimentacaoDTO>> Put(int id, MovimentacaoDTO movimentacaoDTO)
        {
            if (movimentacaoDTO is null || movimentacaoDTO.MovimentacaoId != id)
            {
                _logger.LogWarning("Dados da movimentação inválidos ou ID da movimentação não corresponde ao ID fornecido.");
                return BadRequest("Dados da movimentação inválidos ou ID da movimentação não corresponde ao ID fornecido.");
            }

            // Alterar o histórico mexe no SaldoAtual do produto (estorno do lançamento
            // antigo + aplicação do novo), então o fluxo passa pelo EstoqueService.
            var resultado = await _estoqueService.AtualizarMovimentacaoAsync(id, movimentacaoDTO);

            return ProcessarEscritaDeHistorico(resultado);
        }

        [Authorize(Policy = "SuperAdminOnly")]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<MovimentacaoDTO>> Delete(int id)
        {
            // Excluir estorna do saldo o efeito da movimentação — mesma razão do PUT.
            var resultado = await _estoqueService.ExcluirMovimentacaoAsync(id);

            return ProcessarEscritaDeHistorico(resultado);
        }

        //metodos privates

        // DataInicio/DataFim valem para /pagination e /relatorio. Um intervalo invertido
        // devolveria lista vazia em silêncio, então é rejeitado antes de ir ao banco.
        private ActionResult? ValidarIntervaloDeDatas(MovimentacoesParameters parameters)
        {
            if (parameters.DataInicio.HasValue && parameters.DataFim.HasValue &&
                parameters.DataInicio > parameters.DataFim)
            {
                _logger.LogWarning("Intervalo inválido: DataInicio {DataInicio} é maior que DataFim {DataFim}.",
                                   parameters.DataInicio, parameters.DataFim);
                return BadRequest("DataInicio não pode ser maior que DataFim.");
            }

            return null;
        }

        private ActionResult<IEnumerable<MovimentacaoDTO>> ObterMovimentacoes(IPagedList<Movimentacao> movimentacoes)
        {
            Response.AdicionarHeaderDePaginacao(movimentacoes);

            var movimentacoesDTO = _mapper.Map<IEnumerable<MovimentacaoDTO>>(movimentacoes);
            return Ok(movimentacoesDTO);
        }

        // Registro de movimentação (POST): sucesso vira 201 apontando para o recurso criado.
        private ActionResult<MovimentacaoDTO> ProcessarResultado(EstoqueResult resultado)
        {
            var falha = MapearFalha(resultado);
            if (falha is not null)
                return falha;

            var movimentacaoDTO = _mapper.Map<MovimentacaoDTO>(resultado.Movimentacao);
            return CreatedAtRoute("ObterMovimentacao",
                new { id = movimentacaoDTO.MovimentacaoId }, movimentacaoDTO);
        }

        // Correção de histórico (PUT/DELETE): sucesso vira 200 com a movimentação resultante.
        private ActionResult<MovimentacaoDTO> ProcessarEscritaDeHistorico(EstoqueResult resultado)
        {
            var falha = MapearFalha(resultado);
            if (falha is not null)
                return falha;

            return Ok(_mapper.Map<MovimentacaoDTO>(resultado.Movimentacao));
        }

        // Traduz apenas o desfecho de FALHA para o status HTTP; devolve null no sucesso,
        // que cada endpoint representa de um jeito (201 no POST, 200 no PUT/DELETE).
        private ActionResult? MapearFalha(EstoqueResult resultado) => resultado.Status switch
        {
            EstoqueResultStatus.Sucesso => null,

            EstoqueResultStatus.ProdutoNaoEncontrado or
            EstoqueResultStatus.ColaboradorNaoEncontrado or
            EstoqueResultStatus.MovimentacaoNaoEncontrada => NotFound(resultado.Mensagem),

            EstoqueResultStatus.SaldoInsuficiente or
            EstoqueResultStatus.TipoInvalido or
            EstoqueResultStatus.ColaboradorObrigatorio or
            EstoqueResultStatus.SaldoNegativoAposEstorno => BadRequest(resultado.Mensagem),

            _ => StatusCode(500, "Erro ao processar a movimentação.")
        };
    }
}
