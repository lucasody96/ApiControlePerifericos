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

        // A regra de "precisa repor" tem nome aqui, em vez de ficar só como comparação solta
        // espalhada por quem consulta. NotMapped porque é derivada de duas colunas que já
        // existem — não vira coluna nova nem migration.
        //
        // O Where do ProdutoRepository.GetAbaixoEstoqueMinimoAsync continua com a comparação
        // literal de propósito: o EF traduz expressão para SQL, e não sabe abrir esta
        // propriedade. Mudou o critério aqui, mude lá também — e no frontend.
        [NotMapped]
        public bool AbaixoDoMinimo => SaldoAtual < EstoqueMinimo;

        [JsonIgnore]
        public ICollection<Movimentacao> Movimentacoes { get; set; } = [];
    }
}
