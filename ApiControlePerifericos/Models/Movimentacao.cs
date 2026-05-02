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

        public int ProdutoId { get; set; }
        public int ColaboradorId { get; set; }
        public Produto? Produto { get; set; }
        public Colaborador? Colaborador { get; set; }
    }
}
