namespace ApiControlePerifericos.Interfaces
{
    // Permite à camada de Application (EstoqueService) sinalizar que o cache de
    // produtos ficou obsoleto — sem conhecer o mecanismo de cache (IMemoryCache),
    // que vive na Infrastructure. Implementado por ProdutoCacheInvalidator na Infra.
    public interface IProdutoCacheInvalidator
    {
        void InvalidarProdutos();
    }
}
