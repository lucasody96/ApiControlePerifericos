using Newtonsoft.Json;
using X.PagedList;

namespace ApiControlePerifericos.Extensions
{
    /// <summary>
    /// Ponto único de montagem e escrita do header de metadados de paginação.
    /// Antes o mesmo bloco estava copiado nos quatro endpoints paginados
    /// (Produtos, Colaboradores, Movimentacoes e Usuarios).
    /// </summary>
    public static class PaginacaoResponseExtensions
    {
        /// <summary>
        /// Nome do header. Precisa continuar em WithExposedHeaders na política de CORS,
        /// senão o JS do frontend não consegue lê-lo.
        /// </summary>
        public const string HeaderPaginacao = "X-Pagination";

        /// <summary>
        /// Serializa os metadados da página e os escreve no header <see cref="HeaderPaginacao"/>.
        /// O payload mantém as chaves que o frontend consome: Count, PageSize, PageCount,
        /// TotalItemCount, HasNextPage e HasPreviousPage.
        /// </summary>
        public static void AdicionarHeaderDePaginacao<T>(this HttpResponse response, IPagedList<T> pagina)
        {
            var metadata = new
            {
                pagina.Count,
                pagina.PageSize,
                pagina.PageCount,
                pagina.TotalItemCount,
                pagina.HasNextPage,
                pagina.HasPreviousPage
            };

            response.Headers.Append(HeaderPaginacao, JsonConvert.SerializeObject(metadata));
        }
    }
}
