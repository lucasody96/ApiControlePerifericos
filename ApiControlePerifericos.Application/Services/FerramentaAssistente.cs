namespace ApiControlePerifericos.Services
{
    // Tipos de parâmetro que as consultas do assistente precisam. Objeto aninhado e array
    // ficam de fora de propósito: nenhuma ferramenta de leitura daqui pede isso, e o
    // esquema simples mantém trivial a tradução para o formato do SDK.
    public enum TipoParametroFerramenta
    {
        Texto,
        Inteiro
    }

    // Um parâmetro de entrada da ferramenta. A Descricao é lida pelo modelo — é ela que
    // ensina o que passar aqui, então pesa mais que o nome do parâmetro.
    public record ParametroFerramenta(
        string Nome,
        TipoParametroFerramenta Tipo,
        string Descricao,
        bool Obrigatorio = true);

    // Uma ferramenta que o assistente pode chamar. A Application declara o que existe e o
    // que cada uma faz; a Infrastructure traduz isso para o formato do SDK e executa o
    // delegate quando o modelo pedir.
    //
    // Os argumentos chegam como dicionário de strings já extraído do JSON do modelo: a
    // Application não precisa saber que o transporte é JSON, e cada ferramenta converte o
    // que espera. O retorno é o resultado da consulta serializado como JSON.
    public record FerramentaAssistente(
        string Nome,
        string Descricao,
        IReadOnlyList<ParametroFerramenta> Parametros,
        Func<IReadOnlyDictionary<string, string>, CancellationToken, Task<string>> Executar);
}