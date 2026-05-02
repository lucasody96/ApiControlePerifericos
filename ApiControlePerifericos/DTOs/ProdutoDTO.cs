using System.ComponentModel.DataAnnotations;

namespace ApiControlePerifericos.DTOs
{
    public class ProdutoDTO
    {
        public int ProdutoId { get; set; }

        [Required]
        [StringLength(300)]
        public string? Descricao { get; set; }

        [Range(0, int.MaxValue)]
        public int SaldoAtual { get; set; }

        [Range(0, int.MaxValue)]
        public int EstoqueMinimo { get; set; }
    }
}
