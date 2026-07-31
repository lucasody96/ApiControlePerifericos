using System.Security.Claims;

namespace ApiControlePerifericos.Auth
{
    /// <summary>
    /// Ponto único de resolução da allowlist de super admins (seção de config "SuperAdmins").
    /// É lida em dois lugares — a policy "SuperAdminOnly" (Program.cs) e a proteção de reset
    /// de senha (AuthController.EhSuperAdmin) — que precisam enxergar exatamente a mesma lista.
    /// Antes cada lado aplicava o seu próprio fallback e eles divergiam com a seção ausente.
    ///
    /// Além da lista, o <b>critério de comparação</b> do username também é único e mora aqui:
    /// <see cref="Comparador"/>. O Identity trata username como case-insensitive (compara pela
    /// coluna NormalizedUserName), então a allowlist segue o mesmo critério — uma entrada de
    /// config com caixa diferente do UserName persistido continua valendo.
    /// </summary>
    public static class SuperAdminAllowlist
    {
        public const string SecaoConfiguracao = "SuperAdmins";

        /// <summary>
        /// Tipo do claim que carrega o username no JWT (montado no login pelo AuthController).
        /// </summary>
        public const string TipoClaimUserName = "id";

        /// <summary>
        /// Critério único de comparação de username. Case-insensitive por alinhamento com o
        /// Identity; Ordinal (e não Culture) porque username não é texto de exibição e a
        /// comparação não pode variar com a cultura do servidor.
        /// </summary>
        private static readonly StringComparer Comparador = StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Usada quando a seção está ausente ou vazia. Mantemos um default em vez de falhar
        /// no startup para que uma config faltando não deixe o sistema sem nenhum super admin
        /// (não haveria como promover ninguém depois).
        /// </summary>
        private static readonly string[] PadraoQuandoAusente = ["lucas.ody", "admin"];

        /// <summary>
        /// Devolve os usernames com privilégio de super admin.
        /// </summary>
        public static string[] Resolver(IConfiguration configuration)
        {
            var configurados = configuration.GetSection(SecaoConfiguracao).Get<string[]>();

            return configurados is { Length: > 0 } ? configurados : PadraoQuandoAusente;
        }

        /// <summary>
        /// Indica se o username informado é super admin.
        /// </summary>
        public static bool Contem(IConfiguration configuration, string? userName) =>
            Contem(Resolver(configuration), userName);

        /// <summary>
        /// Sobrecarga para quem já resolveu a allowlist uma única vez (a policy, no startup).
        /// </summary>
        public static bool Contem(IEnumerable<string> allowlist, string? userName) =>
            !string.IsNullOrEmpty(userName) && allowlist.Contains(userName, Comparador);

        /// <summary>
        /// Avalia o claim de username do principal contra a allowlist. Usado pela policy
        /// "SuperAdminOnly" no lugar de RequireClaim, que compara os valores do claim com
        /// StringComparer.Ordinal (case-sensitive) e por isso divergia do EhSuperAdmin.
        /// Como RequireClaim, aceita qualquer um dos claims desse tipo.
        /// </summary>
        public static bool EhSuperAdmin(ClaimsPrincipal? user, IEnumerable<string> allowlist) =>
            user is not null
            && user.FindAll(TipoClaimUserName).Any(claim => Contem(allowlist, claim.Value));
    }
}
