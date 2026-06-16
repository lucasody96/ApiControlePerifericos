using System.ComponentModel.DataAnnotations;

namespace ApiControlePerifericos.DTOs.Estoque
{
    // Ajuste de estoque: subtrai quantidade do saldo por perda/quebra (Tipo 'A').
    public class AjusteEstoqueRequest
    {
        [Required]
        public int ProdutoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
        public int Quantidade { get; set; }
    }
}
