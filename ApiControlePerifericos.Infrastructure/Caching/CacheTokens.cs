using Microsoft.Extensions.Primitives;

namespace ApiControlePerifericos.Caching
{
    // Mantém um CancellationTokenSource por grupo de cache. As entradas do
    // IMemoryCache são vinculadas ao change token do grupo (AddExpirationToken);
    // invalidar o grupo cancela o token — expirando TODAS as entradas daquele
    // recurso de uma só vez, inclusive as variações de paginação, que o
    // IMemoryCache não permite remover por prefixo. Registrado como singleton.
    public class CacheTokens
    {
        private readonly object _lock = new();
        private readonly Dictionary<string, CancellationTokenSource> _fontes = new();

        public IChangeToken ObterToken(string grupo)
        {
            lock (_lock)
            {
                if (!_fontes.TryGetValue(grupo, out var cts) || cts.IsCancellationRequested)
                {
                    cts = new CancellationTokenSource();
                    _fontes[grupo] = cts;
                }
                return new CancellationChangeToken(cts.Token);
            }
        }

        public void Invalidar(string grupo)
        {
            CancellationTokenSource? cts;
            lock (_lock)
            {
                if (!_fontes.TryGetValue(grupo, out cts))
                    return;
                _fontes.Remove(grupo);
            }
            // Cancel dispara sincronamente a expiração das entradas vinculadas.
            // Não descartamos o CTS: o IMemoryCache ainda pode consultar o token
            // durante a remoção, e um token de source descartado lançaria.
            cts.Cancel();
        }
    }
}
