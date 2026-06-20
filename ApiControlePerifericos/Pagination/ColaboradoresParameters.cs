namespace ApiControlePerifericos.Pagination
{
    public class ColaboradoresParameters : QueryStringParameters
    {
        // Filtro opcional de busca por nome (Contains).
        public string? Nome { get; set; }
    }
}
