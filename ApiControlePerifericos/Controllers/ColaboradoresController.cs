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
    public class ColaboradoresController : ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly ILogger<ColaboradoresController> _logger;
        private readonly IMapper _mapper;

        public ColaboradoresController(IUnitOfWork uof, ILogger<ColaboradoresController> logger, IMapper mapper)
        {
            _uof = uof;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ColaboradorDTO>> Get()
        {
            var colaboradores = _uof.ColaboradorRepository.GetAll();
            if (colaboradores is null || !colaboradores.Any())
            {
                _logger.LogInformation("Nenhum colaborador encontrado.");
                return NotFound("Nenhum colaborador encontrado.");
            }

            var colaboradoresDTO = _mapper.Map<IEnumerable<ColaboradorDTO>>(colaboradores);
            return Ok(colaboradoresDTO);
        }

        [HttpGet("{id}", Name = "ObterColaborador")]
        public ActionResult<ColaboradorDTO> Get(int id)
        {
            var colaborador = _uof.ColaboradorRepository.Get(c => c.ColaboradorId == id);
            if (colaborador is null)
            {
                _logger.LogWarning("Colaborador com ID {Id} não encontrado.", id);
                return NotFound($"Colaborador com ID {id} não encontrado.");
            }

            var colaboradorDTO = _mapper.Map<ColaboradorDTO>(colaborador);
            return Ok(colaboradorDTO);
        }

        [HttpGet("pagination")]
        public ActionResult<IEnumerable<ColaboradorDTO>> Get([FromQuery] ColaboradoresParameters colaboradoresParameters)
        {
            var colaboradores = _uof.ColaboradorRepository.GetColaboradores(colaboradoresParameters);
            return ObterColaboradores(colaboradores);
        }

        private ActionResult<IEnumerable<ColaboradorDTO>> ObterColaboradores(PagedList<Colaborador> colaboradores)
        {
            var metadata = new
            {
                colaboradores.TotalCount,
                colaboradores.PageSize,
                colaboradores.CurrentPage,
                colaboradores.TotalPages,
                colaboradores.HasNext,
                colaboradores.HasPrevious
            };

            Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(metadata));
            var colaboradoresDto = _mapper.Map<IEnumerable<ColaboradorDTO>>(colaboradores);
            return Ok(colaboradoresDto);
        }

        [HttpPost]
        public ActionResult<ColaboradorDTO> Post(ColaboradorDTO colaboradorDTO)
        {
            if (colaboradorDTO is null)
            {
                _logger.LogWarning("Dados do colaborador inválidos.");
                return BadRequest("Dados do colaborador inválidos.");
            }

            var colaborador = _mapper.Map<Colaborador>(colaboradorDTO);
            var novoColaborador = _uof.ColaboradorRepository.Create(colaborador);
            _uof.Commit();

            var novoColaboradorDTO = _mapper.Map<ColaboradorDTO>(novoColaborador);
            return CreatedAtRoute("ObterColaborador", new { id = novoColaboradorDTO.ColaboradorId }, novoColaboradorDTO);
        }

        [HttpPut("{id:int}")]
        public ActionResult<ColaboradorDTO> Put(int id, ColaboradorDTO colaboradorDTO)
        {
            if (colaboradorDTO is null || id != colaboradorDTO.ColaboradorId)
            {
                _logger.LogWarning("Dados do colaborador inválidos ou ID do colaborador não corresponde ao ID fornecido.");
                return BadRequest("Dados do colaborador inválidos ou ID do colaborador não corresponde ao ID fornecido.");
            }

            var colaborador = _mapper.Map<Colaborador>(colaboradorDTO);
            var colaboradorAtualizado = _uof.ColaboradorRepository.Update(colaborador);
            _uof.Commit();

            var colaboradorAtualizadoDTO = _mapper.Map<ColaboradorDTO>(colaboradorAtualizado);
            return Ok(colaboradorAtualizadoDTO);
        }

        [HttpDelete("{id:int}")]
        public ActionResult<ColaboradorDTO> Delete(int id)
        {
            var colaborador = _uof.ColaboradorRepository.Get(c => c.ColaboradorId == id);
            if (colaborador is null)
            {
                _logger.LogWarning("Colaborador com ID {Id} não encontrado.", id);
                return NotFound($"Colaborador com ID {id} não encontrado.");
            }

            var colaboradorExcluido = _uof.ColaboradorRepository.Delete(colaborador);
            _uof.Commit();

            var colaboradorExcluidoDTO = _mapper.Map<ColaboradorDTO>(colaboradorExcluido);
            return Ok(colaboradorExcluidoDTO);
        }
    }
}
