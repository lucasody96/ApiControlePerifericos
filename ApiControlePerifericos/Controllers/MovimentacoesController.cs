using ApiControlePerifericos.DTOs;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using AutoMapper;
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

        public MovimentacoesController(IUnitOfWork uof, ILogger<MovimentacoesController> logger, IMapper mapper)
        {
            _uof = uof;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
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
        public async Task<ActionResult<IEnumerable<MovimentacaoDTO>>> Get([FromQuery] MovimentacoesParameters parameters)
        {
            var movimentacoes = await _uof.MovimentacaoRepository.GetMovimentacoesAsync(parameters);
            return ObterMovimentacoes(movimentacoes);
        }

        private ActionResult<IEnumerable<MovimentacaoDTO>> ObterMovimentacoes(IPagedList<Movimentacao> movimentacoes)
        {
            // TODO - Extrair a montagem do metadata para um método
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

            var movimentacoesDTO = _mapper.Map<IEnumerable<MovimentacaoDTO>>(movimentacoes);
            return Ok(movimentacoesDTO);
        }

        [HttpPost]
        public async Task<ActionResult<MovimentacaoDTO>> Post(MovimentacaoDTO movimentacaoDTO)
        {
            if (movimentacaoDTO is null)
            {
                _logger.LogWarning("Dados da movimentação inválidos.");
                return BadRequest("Dados da movimentação inválidos.");
            }

            var movimentacao = _mapper.Map<Movimentacao>(movimentacaoDTO);
            var novaMovimentacao = _uof.MovimentacaoRepository.Create(movimentacao);
            await _uof.CommitAsync();

            var novaMovimentacaoDTO = _mapper.Map<MovimentacaoDTO>(novaMovimentacao);
            return CreatedAtRoute("ObterMovimentacao", new { id = novaMovimentacaoDTO.MovimentacaoId }, novaMovimentacaoDTO);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<MovimentacaoDTO>> Put(int id, MovimentacaoDTO movimentacaoDTO)
        {
            if (movimentacaoDTO is null || movimentacaoDTO.MovimentacaoId != id)
            {
                _logger.LogWarning("Dados da movimentação inválidos ou ID da movimentação não corresponde ao ID fornecido.");
                return BadRequest("Dados da movimentação inválidos ou ID da movimentação não corresponde ao ID fornecido.");
            }

            var movimentacao = _mapper.Map<Movimentacao>(movimentacaoDTO);
            var movimentacaoAtualizada = _uof.MovimentacaoRepository.Update(movimentacao);
            await _uof.CommitAsync();

            var movimentacaoAtualizadaDTO = _mapper.Map<MovimentacaoDTO>(movimentacaoAtualizada);
            return Ok(movimentacaoAtualizadaDTO);
        }

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
    }
}
