using ApiControlePerifericos.DTOs;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

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
        public ActionResult<IEnumerable<MovimentacaoDTO>> Get()
        {
            var movimentacoes = _uof.MovimentacaoRepository.GetAll();
            if (movimentacoes is null || !movimentacoes.Any())
            {
                _logger.LogInformation("Nenhuma movimentação encontrada.");
                return NotFound("Nenhuma movimentação encontrada.");
            }

            var movimentacoesDTO = _mapper.Map<IEnumerable<MovimentacaoDTO>>(movimentacoes);
            return Ok(movimentacoesDTO);
        }

        [HttpGet("{id}", Name = "ObterMovimentacao")]
        public ActionResult<MovimentacaoDTO> Get(int id)
        {
            var movimentacao = _uof.MovimentacaoRepository.Get(m => m.MovimentacaoId == id);
            if (movimentacao is null)
            {
                _logger.LogWarning("Movimentação com ID {Id} não encontrada.", id);
                return NotFound($"Movimentação com ID {id} não encontrada.");
            }

            var movimentacaoDTO = _mapper.Map<MovimentacaoDTO>(movimentacao);
            return Ok(movimentacaoDTO);
        }

        [HttpGet("pagination")]
        public ActionResult<IEnumerable<MovimentacaoDTO>> Get([FromQuery] MovimentacoesParameters movimentacoesParameters)
        {
            var movimentacoes = _uof.MovimentacaoRepository.GetMovimentacoes(movimentacoesParameters);
            return ObterMovimentacoes(movimentacoes);
        }

        private ActionResult<IEnumerable<MovimentacaoDTO>> ObterMovimentacoes(PagedList<Movimentacao> movimentacoes)
        {
            var metadata = new
            {
                movimentacoes.TotalCount,
                movimentacoes.PageSize,
                movimentacoes.CurrentPage,
                movimentacoes.TotalPages,
                movimentacoes.HasNext,
                movimentacoes.HasPrevious
            };

            Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(metadata));
            var movimentacoesDto = _mapper.Map<IEnumerable<MovimentacaoDTO>>(movimentacoes);
            return Ok(movimentacoesDto);
        }

        [HttpPost]
        public ActionResult<MovimentacaoDTO> Post(MovimentacaoDTO movimentacaoDTO)
        {
            if (movimentacaoDTO is null)
            {
                _logger.LogWarning("Dados da movimentação inválidos.");
                return BadRequest("Dados da movimentação inválidos.");
            }

            var movimentacao = _mapper.Map<Movimentacao>(movimentacaoDTO);
            var novaMovimentacao = _uof.MovimentacaoRepository.Create(movimentacao);
            _uof.Commit();

            var novaMovimentacaoDTO = _mapper.Map<MovimentacaoDTO>(novaMovimentacao);
            return CreatedAtRoute("ObterMovimentacao", new { id = novaMovimentacaoDTO.MovimentacaoId }, novaMovimentacaoDTO);
        }

        [HttpPut("{id:int}")]
        public ActionResult<MovimentacaoDTO> Put(int id, MovimentacaoDTO movimentacaoDTO)
        {
            if (movimentacaoDTO is null || movimentacaoDTO.MovimentacaoId != id)
            {
                _logger.LogWarning("Dados da movimentação inválidos ou ID da movimentação não corresponde ao ID fornecido.");
                return BadRequest("Dados da movimentação inválidos ou ID da movimentação não corresponde ao ID fornecido.");
            }

            var movimentacao = _mapper.Map<Movimentacao>(movimentacaoDTO);
            var movimentacaoAtualizada = _uof.MovimentacaoRepository.Update(movimentacao);
            _uof.Commit();

            var movimentacaoAtualizadaDTO = _mapper.Map<MovimentacaoDTO>(movimentacaoAtualizada);
            return Ok(movimentacaoAtualizadaDTO);
        }

        [HttpDelete("{id:int}")]
        public ActionResult<MovimentacaoDTO> Delete(int id)
        {
            var movimentacao = _uof.MovimentacaoRepository.Get(m => m.MovimentacaoId == id);
            if (movimentacao is null)
            {
                _logger.LogWarning("Movimentação com ID {Id} não encontrada.", id);
                return NotFound($"Movimentação com ID {id} não encontrada.");
            }

            var movimentacaoExcluida = _uof.MovimentacaoRepository.Delete(movimentacao);
            _uof.Commit();

            var movimentacaoExcluidaDTO = _mapper.Map<MovimentacaoDTO>(movimentacaoExcluida);
            return Ok(movimentacaoExcluidaDTO);
        }
    }
}
