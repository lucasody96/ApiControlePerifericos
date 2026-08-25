using ApiControlePerifericos.DTOs.Assistente;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;


namespace ApiControlePerifericos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // Qualquer usuário autenticado pergunta, e é de propósito: o caso de uso principal do
    // assistente é dúvida de manual, justamente de quem não é admin.
    //
    // O que separa admin de não-admin é a LISTA DE FERRAMENTAS, não o endpoint (issue #49).
    // As consultas de produto acompanham o ProdutosController, que também é [Authorize]
    // simples; as de movimentação só entram para Admin, porque o MovimentacoesController
    // inteiro é AdminOnly e o assistente não pode ser porta lateral para o que ele protege.
    [Authorize]
    public class AssistenteController : ControllerBase
    {
        private readonly IAssistenteService _assistenteService;

        public AssistenteController(IAssistenteService assistenteService)
        {
            _assistenteService = assistenteService;
        }

        [HttpPost("perguntar")]
        [EnableRateLimiting("assistente")]
        public async Task<ActionResult<RespostaDTO>> Perguntar([FromBody] PerguntaDTO dto, CancellationToken cancellationToken)
        {
            var resultado = await _assistenteService.ResponderAsync(
                dto.Pergunta, User.IsInRole("Admin"), cancellationToken);

            if (!resultado.Sucesso)
                return MapearFalha(resultado);

            return Ok(new RespostaDTO { Resposta = resultado.Resposta! });
        }

        // Traduz o desfecho do serviço em status HTTP, como o MovimentacoesController.
        private ActionResult MapearFalha(AssistenteResult resultado) => resultado.Status
        switch
        {
            AssistenteResultStatus.PerguntaVazia or
            AssistenteResultStatus.PerguntaMuitoLonga => BadRequest(resultado.Mensagem),

            // A API externa falhou: é indisponibilidade temporária, não erro do cliente.
            AssistenteResultStatus.FalhaNaIA => StatusCode(StatusCodes.Status503ServiceUnavailable, resultado.Mensagem),

            _ => StatusCode(StatusCodes.Status500InternalServerError, "Falha inesperada ao consultar o assistente.")

        };
    }
}
