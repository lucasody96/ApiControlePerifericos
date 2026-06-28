using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ApiControlePerifericos.Models
{
    [Table("Produtos")]
    public class Produto
    {
        [Key]
        public int ProdutoId { get; set; }

        [Required]
        [StringLength(300)]
        public string? Descricao { get; set; }

        [Range(0, int.MaxValue)]
        public int SaldoAtual { get; set; }

        [Range(0, int.MaxValue)]
        public int EstoqueMinimo { get; set; }

        [JsonIgnore]
        public ICollection<Movimentacao> Movimentacoes { get; set; } = [];
    }
}
