using System.ComponentModel.DataAnnotations;

namespace ApiControlePerifericos.DTOs.Identity
{
    public class AtualizarRolesRequest
    {
        // Conjunto final de roles do usuário. O controller adiciona as que
        // faltam e remove as que sobram em relação às roles atuais.
        [Required(ErrorMessage = "A lista de roles é obrigatória")]
        public List<string> Roles { get; set; } = [];
    }
}
