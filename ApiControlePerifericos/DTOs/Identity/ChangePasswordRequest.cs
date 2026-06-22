using System.ComponentModel.DataAnnotations;

namespace ApiControlePerifericos.DTOs.Identity
{
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "A senha atual é obrigatória.")]
        public string? CurrentPassword { get; set; }

        [Required(ErrorMessage = "A nova senha é obrigatória.")]
        public string? NewPassword { get; set; }
    }
}
