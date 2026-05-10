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
        public async Task<ActionResult<IEnumerable<ColaboradorDTO>>> Get()
        {
            var colaboradores = await _uof.ColaboradorRepository.GetAllAsync();
            if (colaboradores is null || !colaboradores.Any())
            {
                _logger.LogInformation("Nenhum colaborador encontrado.");
                return NotFound("Nenhum colaborador encontrado.");
            }

            var colaboradoresDTO = _mapper.Map<IEnumerable<ColaboradorDTO>>(colaboradores);
            return Ok(colaboradoresDTO);
        }

        [HttpGet("{id}", Name = "ObterColaborador")]
        public async Task<ActionResult<ColaboradorDTO>> Get(int id)
        {
            var colaborador = await _uof.ColaboradorRepository.GetAsync(c => c.ColaboradorId == id);
            if (colaborador is null)
            {
                _logger.LogWarning("Colaborador com ID {Id} não encontrado.", id);
                return NotFound($"Colaborador com ID {id} não encontrado.");
            }

            var colaboradorDTO = _mapper.Map<ColaboradorDTO>(colaborador);
            return Ok(colaboradorDTO);
        }

        [HttpGet("pagination")]
        public async Task<ActionResult<IEnumerable<ColaboradorDTO>>> Get([FromQuery] ColaboradoresParameters parameters)
        {
            var colaboradores = await _uof.ColaboradorRepository.GetColaboradoresAsync(parameters);
            return ObterColaboradores(colaboradores);
        }

        private ActionResult<IEnumerable<ColaboradorDTO>> ObterColaboradores(IPagedList<Colaborador> colaboradores)
        {
            // TODO - Extrair a montagem do metadata para um método
            var metadata = new
            {
                colaboradores.Count,
                colaboradores.PageSize,
                colaboradores.PageCount,
                colaboradores.TotalItemCount,
                colaboradores.HasNextPage,
                colaboradores.HasPreviousPage
            };

            Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(metadata));

            var colaboradoresDTO = _mapper.Map<IEnumerable<ColaboradorDTO>>(colaboradores);
            return Ok(colaboradoresDTO);
        }

        [HttpPost]
        public async Task<ActionResult<ColaboradorDTO>> Post(ColaboradorDTO colaboradorDTO)
        {
            if (colaboradorDTO is null)
            {
                _logger.LogWarning("Dados do colaborador inválidos.");
                return BadRequest("Dados do colaborador inválidos.");
            }

            var colaborador = _mapper.Map<Colaborador>(colaboradorDTO);
            var novoColaborador = _uof.ColaboradorRepository.Create(colaborador);
            await _uof.CommitAsync();

            var novoColaboradorDTO = _mapper.Map<ColaboradorDTO>(novoColaborador);
            return CreatedAtRoute("ObterColaborador", new { id = novoColaboradorDTO.ColaboradorId }, novoColaboradorDTO);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ColaboradorDTO>> Put(int id, ColaboradorDTO colaboradorDTO)
        {
            if (colaboradorDTO is null || colaboradorDTO.ColaboradorId != id)
            {
                _logger.LogWarning("Dados do colaborador inválidos ou ID do colaborador não corresponde ao ID fornecido.");
                return BadRequest("Dados do colaborador inválidos ou ID do colaborador não corresponde ao ID fornecido.");
            }

            var existe = await _uof.ColaboradorRepository.GetAsync(c => c.ColaboradorId == id);
            if (existe is null)
            {
                _logger.LogWarning("Colaborador com ID {Id} não encontrado.", id);
                return NotFound($"Colaborador com ID {id} não encontrado.");
            }

            var colaborador = _mapper.Map<Colaborador>(colaboradorDTO);
            var colaboradorAtualizado = _uof.ColaboradorRepository.Update(colaborador);
            await _uof.CommitAsync();

            var colaboradorAtualizadoDTO = _mapper.Map<ColaboradorDTO>(colaboradorAtualizado);
            return Ok(colaboradorAtualizadoDTO);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ColaboradorDTO>> Delete(int id)
        {
            var colaborador = await _uof.ColaboradorRepository.GetAsync(c => c.ColaboradorId == id);
            if (colaborador is null)
            {
                _logger.LogWarning("Colaborador com ID {Id} não encontrado.", id);
                return NotFound($"Colaborador com ID {id} não encontrado.");
            }

            var colaboradorExcluido = _uof.ColaboradorRepository.Delete(colaborador);
            await _uof.CommitAsync();

            var colaboradorExcluidoDTO = _mapper.Map<ColaboradorDTO>(colaboradorExcluido);
            return Ok(colaboradorExcluidoDTO);
        }
    }
}
