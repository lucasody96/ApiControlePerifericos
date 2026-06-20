using ApiControlePerifericos.DTOs;
using ApiControlePerifericos.DTOs.Estoque;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using ApiControlePerifericos.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
            var movimentacoes = await _uof.MovimentacaoRepository.GetMovimentacoesAsync(parameters);
            return ObterMovimentacoes(movimentacoes);
        }

        [HttpGet("relatorio")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<IEnumerable<MovimentacaoRelatorioDTO>>> GetRelatorio([FromQuery] MovimentacoesParameters parameters)
        {
            if (parameters.DataInicio.HasValue && parameters.DataFim.HasValue && parameters.DataInicio > parameters.DataFim)
                return BadRequest("DataInicio não pode ser maior que DataFim.");

            var movimentacoes = await _uof.MovimentacaoRepository.GetRelatorioAsync(parameters);

            if (movimentacoes is null || !movimentacoes.Any())
            {
                _logger.LogInformation("Nenhuma movimentação encontrada para o relatório.");
                return NotFound("Nenhuma movimentação encontrada para o relatório.");
            }

            ObterNovoMetadata(movimentacoes);

            var relatorioDTO = _mapper.Map<IEnumerable<MovimentacaoRelatorioDTO>>(movimentacoes);

            return Ok(relatorioDTO);
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

            var existe = await _uof.MovimentacaoRepository.ExistsAsync(m => m.MovimentacaoId == id);
            if (!existe)
            {
                _logger.LogWarning("Movimentação com ID {Id} não encontrada.", id);
                return NotFound($"Movimentação com ID {id} não encontrada.");
            }

            var movimentacao = _mapper.Map<Movimentacao>(movimentacaoDTO);
            var movimentacaoAtualizada = _uof.MovimentacaoRepository.Update(movimentacao);
            await _uof.CommitAsync();

            var movimentacaoAtualizadaDTO = _mapper.Map<MovimentacaoDTO>(movimentacaoAtualizada);
            return Ok(movimentacaoAtualizadaDTO);
        }

        [Authorize(Policy = "SuperAdminOnly")]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<MovimentacaoDTO>> Delete(int id)
        {
            var movimentacao = await _uof.MovimentacaoRepository.GetAsync(m => m.MovimentacaoId == id);
            if (movimentacao is null)
            {
                _logger.LogWarning("Movimentação com ID {Id} não encontrada.", id);
                return NotFound($"Movimentação com ID {id} não encontrada.");
            }

            var movimentacaoExcluida = _uof.MovimentacaoRepository.Delete(movimentacao);
            await _uof.CommitAsync();

            var movimentacaoExcluidaDTO = _mapper.Map<MovimentacaoDTO>(movimentacaoExcluida);
            return Ok(movimentacaoExcluidaDTO);
        }

        //metodos privates
        private void ObterNovoMetadata(IPagedList<Movimentacao> movimentacoes)
        {
            var metadata = new
            {
                movimentacoes.Count,
                movimentacoes.PageSize,
                movimentacoes.PageCount,
                movimentacoes.TotalItemCount,
                movimentacoes.HasNextPage,
                movimentacoes.HasPreviousPage
            };

            Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(metadata));
        }

        private ActionResult<IEnumerable<MovimentacaoDTO>> ObterMovimentacoes(IPagedList<Movimentacao> movimentacoes)
        {
            ObterNovoMetadata(movimentacoes);

            var movimentacoesDTO = _mapper.Map<IEnumerable<MovimentacaoDTO>>(movimentacoes);
            return Ok(movimentacoesDTO);
        }

        // Mapeia o desfecho da operação de estoque para o status HTTP adequado.
        private ActionResult<MovimentacaoDTO> ProcessarResultado(EstoqueResult resultado)
        {
            switch (resultado.Status)
            {
                case EstoqueResultStatus.ProdutoNaoEncontrado:
                case EstoqueResultStatus.ColaboradorNaoEncontrado:
                    return NotFound(resultado.Mensagem);

                case EstoqueResultStatus.SaldoInsuficiente:
                    return BadRequest(resultado.Mensagem);

                case EstoqueResultStatus.Sucesso:
                    var movimentacaoDTO = _mapper.Map<MovimentacaoDTO>(resultado.Movimentacao);
                    return CreatedAtRoute("ObterMovimentacao",
                        new { id = movimentacaoDTO.MovimentacaoId }, movimentacaoDTO);

                default:
                    return StatusCode(500, "Erro ao processar a movimentação.");
            }
        }
    }
}
