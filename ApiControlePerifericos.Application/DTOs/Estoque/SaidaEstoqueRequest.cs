using System.ComponentModel.DataAnnotations;

namespace ApiControlePerifericos.DTOs.Estoque
{
    // Saída de estoque: retira quantidade do saldo do produto por um colaborador (Tipo 'S').
    public class SaidaEstoqueRequest
    {
        [Required]
        public int ProdutoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
        public int Quantidade { get; set; }

        // Obrigatório: quem retirou o periférico.
        [Required]
        public int ColaboradorId { get; set; }
    }
}
