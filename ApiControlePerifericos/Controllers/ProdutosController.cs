using ApiControlePerifericos.DTOs;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace ApiControlePerifericos.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    public class ProdutosController: ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly ILogger<ProdutosController> _logger;
        private readonly IMapper _mapper;

        public ProdutosController(IUnitOfWork uof, ILogger<ProdutosController> logger, IMapper mapper)
        {
            _uof = uof;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ProdutoDTO>> Get()
        {
            var produtos = _uof.ProdutoRepository.GetAll();
            if (produtos == null || !produtos.Any())
            {
                _logger.LogInformation("Nenhum produto encontrado.");
                return NotFound("Nenhum produto encontrado.");
            }
            var produtosDTO = _mapper.Map<IEnumerable<ProdutoDTO>>(produtos);
            return Ok(produtosDTO);
        }

        [HttpGet("{id}", Name = "ObterProduto")]
        public ActionResult<ProdutoDTO> Get(int id)
        {
            var produto = _uof.ProdutoRepository.Get(p => p.ProdutoId == id);
            if (produto == null)
            {
                _logger.LogWarning($"Produto com ID {id} não encontrado.");
                return NotFound($"Produto com ID {id} não encontrado.");
            }
            var produtoDTO = _mapper.Map<ProdutoDTO>(produto);
            return Ok(produtoDTO);
        }

        [HttpPost]
        public ActionResult<ProdutoDTO> Post(ProdutoDTO produtoDto)
        {
            if (produtoDto is null)
            {
                _logger.LogWarning($"Dados do produto inválidos.");
                return BadRequest("Dados do produto inválidos.");
            }
            var produto = _mapper.Map<Produto>(produtoDto);
            _uof.ProdutoRepository.Create(produto);
            _uof.Commit();

            var novoprodutoDto = _mapper.Map<ProdutoDTO>(produto);
            return CreatedAtRoute("ObterProduto", new { id = novoprodutoDto.ProdutoId }, novoprodutoDto);
        }

        [HttpPut("{id:int}")]
        public ActionResult<ProdutoDTO> Put(int id, ProdutoDTO produtoDto)
        {
            if (produtoDto is null || produtoDto.ProdutoId != id)
            {
                _logger.LogWarning($"Dados do produto inválidos ou produto não encontrado.");
                return BadRequest("Dados do produto inválidos ou produto não encontrado.");
            }
            var produto = _mapper.Map<Produto>(produtoDto);
            _uof.ProdutoRepository.Update(produto);
            _uof.Commit();
            var produtoAtualizadoDto = _mapper.Map<ProdutoDTO>(produto);
            return Ok(produtoAtualizadoDto);
        }

        [HttpDelete("{id}")]
        public ActionResult<ProdutoDTO> Delete(int id)
        {
            var produto = _uof.ProdutoRepository.Get(p => p.ProdutoId == id);
            if (produto is null)
            {
                _logger.LogWarning($"Produto com ID {id} não encontrado.");
                return NotFound($"Produto com ID {id} não encontrado.");
            }

            var produtoExcluido = _uof.ProdutoRepository.Delete(produto);
            _uof.Commit();

            var produtoExcluidoDto = _mapper.Map<ProdutoDTO>(produtoExcluido);
            return Ok(produtoExcluidoDto);
        }
    }
}
