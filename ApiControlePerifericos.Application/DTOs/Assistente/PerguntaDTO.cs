using ApiControlePerifericos.Services;
using System.ComponentModel.DataAnnotations;

namespace ApiControlePerifericos.DTOs.Assistente
{
    public class PerguntaDTO
    {
        [Required(ErrorMessage = "Informe uma pergunta.")]
        [StringLength(AssistenteService.TamanhoMaximoPergunta)]
        public string? Pergunta { get; set; }
    }
}
