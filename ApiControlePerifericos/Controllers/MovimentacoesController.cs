using ApiControlePerifericos.DTOs;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

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
        public ActionResult<IEnumerable<Movimentacao>> Get()
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
                _logger.LogWarning($"Movimentação com ID {id} não encontrada.");
                return NotFound($"Movimentação com ID {id} não encontrada.");
            }
            var movimentacaoDTO = _mapper.Map<MovimentacaoDTO>(movimentacao);
            return Ok(movimentacaoDTO);
        }

        [HttpPost]
        public ActionResult<MovimentacaoDTO> Post(MovimentacaoDTO movimentacaoDto)
        {
            if (movimentacaoDto is null)
            {
                _logger.LogWarning($"Dados da movimentação inválidos.");
                return BadRequest("Dados da movimentação inválidos.");
            }
            var movimentacao = _mapper.Map<Movimentacao>(movimentacaoDto); 
            var novaMovimentacao = _uof.MovimentacaoRepository.Create(movimentacao);
            _uof.Commit();
            var novaMovimentacaoDTO = _mapper.Map<MovimentacaoDTO>(novaMovimentacao);
            return CreatedAtRoute("ObterMovimentacao", new { id = novaMovimentacaoDTO.MovimentacaoId }, novaMovimentacaoDTO);
        }

        [HttpPut("{id:int}")]
        public ActionResult<MovimentacaoDTO> Put(int id, MovimentacaoDTO movimentacaoDto)
        {
            if (movimentacaoDto is null || movimentacaoDto.MovimentacaoId != id)
            {
                _logger.LogWarning($"Dados da movimentação inválidos ou ID da movimentação não corresponde ao ID fornecido.");
                return BadRequest("Dados da movimentação inválidos ou ID da movimentação não corresponde ao ID fornecido.");
            }

            var movimentacao = _mapper.Map<Movimentacao>(movimentacaoDto);
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
                _logger.LogWarning($"Movimentação com ID {id} não encontrada.");
                return NotFound($"Movimentação com ID {id} não encontrada.");
            }

            var movimentacaoExcluída = _uof.MovimentacaoRepository.Delete(movimentacao);
            _uof.Commit();
            var movimentacaoExcluídaDTO = _mapper.Map<MovimentacaoDTO>(movimentacaoExcluída);
            return Ok(movimentacaoExcluídaDTO);
        }
    }
}
