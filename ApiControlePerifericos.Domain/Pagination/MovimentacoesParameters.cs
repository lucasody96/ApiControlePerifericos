namespace ApiControlePerifericos.Pagination
{
    public class MovimentacoesParameters : QueryStringParameters
    {
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public string? DescricaoProduto { get; set; }
        public string? NomeColaborador { get; set; }

        // Filtro por id: quando quem chama já conhece o registro (a tela seleciona o
        // item numa lista), o id evita a ambiguidade do filtro por texto, em que dois
        // colaboradores de nomes parecidos caem no mesmo resultado.
        public int? ProdutoId { get; set; }
        public int? ColaboradorId { get; set; }
    }
}
