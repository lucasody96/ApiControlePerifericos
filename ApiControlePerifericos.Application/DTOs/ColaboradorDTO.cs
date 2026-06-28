using System.ComponentModel.DataAnnotations;

namespace ApiControlePerifericos.DTOs
{
    public class ColaboradorDTO
    {
        public int ColaboradorId { get; set; }

        [Required]
        [StringLength(80)]
        public string? Nome { get; set; }
    }
}
