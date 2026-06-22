namespace ApiControlePerifericos.DTOs.Identity
{
    // Representa um usuário da aplicação na listagem/gestão.
    public class UsuarioResponse
    {
        public string Id { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string[] Roles { get; set; } = [];
    }
}
