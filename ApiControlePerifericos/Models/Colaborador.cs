using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ApiControlePerifericos.Models
{
    [Table("Colaboradores")]
    public class Colaborador
    {
        [Key]
        public int ColaboradorId { get; set; }

        [Required]
        [StringLength(80)]
        public string? Nome { get; set; }

        [JsonIgnore]
        public ICollection<Movimentacao> Movimentacoes { get; set; } = [];
    }
}
