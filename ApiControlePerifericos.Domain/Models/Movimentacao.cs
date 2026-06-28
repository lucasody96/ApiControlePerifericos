using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiControlePerifericos.Models
{
    [Table("Movimentacoes")]
    public class Movimentacao
    {
        [Key]
        public int MovimentacaoId { get; set; }

        public char Tipo { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantidade { get; set; }

        public DateTime? DataMovimentacao { get; set; }

        // Quem operou o movimento (UserName do JWT). Preenchido em toda movimentação.
        [StringLength(256)]
        public string? RegistradoPor { get; set; }

        public int ProdutoId { get; set; }

        // Nulo em entrada/ajuste; preenchido na saída (quem retirou o periférico).
        public int? ColaboradorId { get; set; }
        public Produto? Produto { get; set; }
        public Colaborador? Colaborador { get; set; }
    }
}
