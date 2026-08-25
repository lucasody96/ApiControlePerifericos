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

        // Tipo da movimentação: 'E' entrada, 'S' saída, 'A' ajuste. Normalizado para
        // maiúscula na entrada porque o assistente pode mandar 's' minúsculo e o banco
        // grava sempre maiúsculo — sem isto o filtro casaria com nada, em silêncio.
        // Quem valida se a letra existe é o controller; aqui só se acerta a caixa.
        private char? _tipo;
        public char? Tipo
        {
            get => _tipo;
            set => _tipo = value.HasValue ? char.ToUpperInvariant(value.Value) : null;
        }
    }
}
