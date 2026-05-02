using System.ComponentModel.DataAnnotations;

namespace ApiControlePerifericos.DTOs
{
    public class MovimentacaoDTO
    {
        public int MovimentacaoId { get; set; }

        public char Tipo { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantidade { get; set; }

        public DateTime? DataMovimentacao { get; set; }

        public int ProdutoId { get; set; }
        public int ColaboradorId { get; set; }
    }
}
