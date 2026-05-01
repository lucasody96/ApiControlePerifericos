using ApiControlePerifericos.DTOs;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

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
                _logger.LogWarning($"Colaborador com ID {id} não encontrado.");
                return NotFound($"Colaborador com ID {id} não encontrado.");
            }
            var colaboradorDTO = _mapper.Map<ColaboradorDTO>(colaborador);
            return Ok(colaboradorDTO);
        }

        [HttpPost]
        public ActionResult<ColaboradorDTO> Post(ColaboradorDTO colaboradorDto)
        {
            if (colaboradorDto is null)
            {
                _logger.LogWarning($"Dados do colaborador inválidos.");
                return BadRequest("Dados do colaborador inválidos.");
            }
            var colaborador = _mapper.Map<Colaborador>(colaboradorDto);
            var novoColaborador = _uof.ColaboradorRepository.Create(colaborador);
            _uof.Commit();

            var novoColaboradorDTO = _mapper.Map<ColaboradorDTO>(novoColaborador);
            return CreatedAtRoute("ObterColaborador", new { id = novoColaboradorDTO.ColaboradorId }, novoColaboradorDTO);
        }

        [HttpPut("{id:int}")]
        public ActionResult<ColaboradorDTO> Put(int id, ColaboradorDTO colaboradorDto)
        {
            if (colaboradorDto is null || id != colaboradorDto.ColaboradorId)
            {
                _logger.LogWarning($"Dados do colaborador inválidos ou ID do colaborador não corresponde ao ID fornecido.");
                return BadRequest("Dados do colaborador inválidos ou ID do colaborador não corresponde ao ID fornecido.");
            }

            var colaborador = _mapper.Map<Colaborador>(colaboradorDto);
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
                _logger.LogWarning($"Colaborador com ID {id} não encontrado.");
                return NotFound($"Colaborador com ID {id} não encontrado.");
            }

            var colaboradorExcluido = _uof.ColaboradorRepository.Delete(colaborador);
            _uof.Commit();

            var colaboradorExcluidoDTO = _mapper.Map<ColaboradorDTO>(colaboradorExcluido);
            return Ok(colaboradorExcluidoDTO);
        }
    }
}
