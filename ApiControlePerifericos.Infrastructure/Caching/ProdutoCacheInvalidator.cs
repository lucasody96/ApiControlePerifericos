using ApiControlePerifericos.Interfaces;

namespace ApiControlePerifericos.Caching
{
    // Implementa a abstração da Application: a movimentação de estoque altera o
    // SaldoAtual do produto (sem passar pelo Update do repositório), então o
    // EstoqueService chama InvalidarProdutos após o commit para expirar o cache.
    public class ProdutoCacheInvalidator : IProdutoCacheInvalidator
    {
        private readonly CacheTokens _tokens;

        public ProdutoCacheInvalidator(CacheTokens tokens) => _tokens = tokens;

        public void InvalidarProdutos() => _tokens.Invalidar(CacheGrupos.Produtos);
    }
}
