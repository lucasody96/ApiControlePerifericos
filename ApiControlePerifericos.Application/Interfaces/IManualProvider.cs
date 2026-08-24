namespace ApiControlePerifericos.Interfaces
{
    // Fonte do texto do manual. A Application decide que o manual é o contexto do
    // assistente; de onde ele vem (arquivo, recurso embutido, banco) é da Infrastructure.
    public interface IManualProvider
    {
        string ObterConteudo();
    }
}
