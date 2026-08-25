namespace ApiControlePerifericos.Models
{
    // Os três tipos que uma movimentação pode ter. A regra mora no Domain porque três
    // camadas precisam dela: o EstoqueService ao gravar, o controller ao validar o filtro
    // e o assistente ao traduzir o que o modelo mandou.
    public static class TipoMovimentacao
    {
        public const char Entrada = 'E';
        public const char Saida = 'S';
        public const char Ajuste = 'A';

        public static bool EhValido(char tipo) =>
            tipo is Entrada or Saida or Ajuste;

        // Mensagem única: o erro do PUT e o erro do filtro ensinam os mesmos três valores.
        public static string DescreverValoresAceitos() =>
            $"Use '{Entrada}' (entrada), '{Saida}' (saída) ou '{Ajuste}' (ajuste).";
    }
}
