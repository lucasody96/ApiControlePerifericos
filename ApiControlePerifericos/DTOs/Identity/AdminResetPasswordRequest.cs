using System.ComponentModel.DataAnnotations;

namespace ApiControlePerifericos.DTOs.Identity
{
    public class AdminResetPasswordRequest
    {
        [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "A nova senha é obrigatória.")]
        public string? NewPassword { get; set; }
    }
}
