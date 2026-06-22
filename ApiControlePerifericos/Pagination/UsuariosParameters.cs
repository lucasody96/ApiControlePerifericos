namespace ApiControlePerifericos.Pagination
{
    public class UsuariosParameters : QueryStringParameters
    {
        // Filtro opcional por username ou e-mail (case-insensitive).
        public string? Busca { get; set; }
    }
}
