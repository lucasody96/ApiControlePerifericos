using System.ComponentModel.DataAnnotations;

namespace ApiControlePerifericos.DTOs.Estoque
{
    // Entrada de estoque: adiciona quantidade ao saldo do produto (Tipo 'E').
    public class EntradaEstoqueRequest
    {
        [Required]
        public int ProdutoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
        public int Quantidade { get; set; }
    }
}
