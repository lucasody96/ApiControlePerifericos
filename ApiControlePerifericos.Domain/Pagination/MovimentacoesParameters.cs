namespace ApiControlePerifericos.Pagination
{
    public class MovimentacoesParameters : QueryStringParameters
    {
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public string? DescricaoProduto { get; set; }
        public string? NomeColaborador { get; set; }
    }
}
