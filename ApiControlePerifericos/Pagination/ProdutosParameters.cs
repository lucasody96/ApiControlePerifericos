namespace ApiControlePerifericos.Pagination
{
    public class ProdutosParameters : QueryStringParameters
    {
        // Filtro opcional de busca por descrição (Contains).
        public string? Descricao { get; set; }
    }
}
