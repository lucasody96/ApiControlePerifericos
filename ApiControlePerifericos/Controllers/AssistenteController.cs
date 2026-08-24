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
    // Qualquer usuário autenticado pergunta — o manual descreve o sistema inteiro, não há
    // dado de estoque na resposta.
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
            var resultado = await _assistenteService.ResponderAsync(dto.Pergunta, cancellationToken);

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
