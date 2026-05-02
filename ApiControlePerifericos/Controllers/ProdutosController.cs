using ApiControlePerifericos.DTOs;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace ApiControlePerifericos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProdutosController : ControllerBase
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
            if (produtos is null || !produtos.Any())
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
            if (produto is null)
            {
                _logger.LogWarning($"Produto com ID {id} não encontrado.");
                return NotFound($"Produto com ID {id} não encontrado.");
            }
            var produtoDTO = _mapper.Map<ProdutoDTO>(produto);
            return Ok(produtoDTO);
        }

        [HttpPost]
        public ActionResult<ProdutoDTO> Post(ProdutoDTO produtoDTO)
        {
            if (produtoDTO is null)
            {
                _logger.LogWarning("Dados do produto inválidos.");
                return BadRequest("Dados do produto inválidos.");
            }
            var produto = _mapper.Map<Produto>(produtoDTO);
            var novoProduto = _uof.ProdutoRepository.Create(produto);
            _uof.Commit();

            var novoProdutoDTO = _mapper.Map<ProdutoDTO>(novoProduto);
            return CreatedAtRoute("ObterProduto", new { id = novoProdutoDTO.ProdutoId }, novoProdutoDTO);
        }

        [HttpPut("{id:int}")]
        public ActionResult<ProdutoDTO> Put(int id, ProdutoDTO produtoDTO)
        {
            if (produtoDTO is null || produtoDTO.ProdutoId != id)
            {
                _logger.LogWarning("Dados do produto inválidos ou produto não encontrado.");
                return BadRequest("Dados do produto inválidos ou produto não encontrado.");
            }
            var produto = _mapper.Map<Produto>(produtoDTO);
            var produtoAtualizado = _uof.ProdutoRepository.Update(produto);
            _uof.Commit();
            var produtoAtualizadoDTO = _mapper.Map<ProdutoDTO>(produtoAtualizado);
            return Ok(produtoAtualizadoDTO);
        }

        [HttpDelete("{id:int}")]
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

            var produtoExcluidoDTO = _mapper.Map<ProdutoDTO>(produtoExcluido);
            return Ok(produtoExcluidoDTO);
        }
    }
}
