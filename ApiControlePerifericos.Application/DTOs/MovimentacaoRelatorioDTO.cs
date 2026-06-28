namespace ApiControlePerifericos.DTOs
{
    public class MovimentacaoRelatorioDTO
    {
        public int MovimentacaoId { get; set; }
        public char Tipo { get; set; }
        public string? TipoDescricao { get; set; }
        public int Quantidade { get; set; }
        public DateTime? DataMovimentacao { get; set; }
        public string? RegistradoPor { get; set; }

        //Ao invés de só o id, o nome legível

        public int ProdutoId { get; set; }
        public string? ProdutoDescricao { get; set; }

        public int? ColaboradorId { get; set; }
        public string? ColaboradorNome { get; set; }  //null em entrada/ajuste
    }
}
