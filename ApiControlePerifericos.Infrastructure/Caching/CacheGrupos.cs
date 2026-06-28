namespace ApiControlePerifericos.Caching
{
    // Grupos de cache por recurso. Cada grupo tem um token de expiração próprio
    // (ver CacheTokens), usado para invalidar de uma vez todas as suas entradas.
    public static class CacheGrupos
    {
        public const string Produtos = "produtos";
        public const string Colaboradores = "colaboradores";
    }
}
