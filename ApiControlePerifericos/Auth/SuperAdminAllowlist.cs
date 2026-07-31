namespace ApiControlePerifericos.Auth
{
    /// <summary>
    /// Ponto único de resolução da allowlist de super admins (seção de config "SuperAdmins").
    /// É lida em dois lugares — a policy "SuperAdminOnly" (Program.cs) e a proteção de reset
    /// de senha (AuthController.EhSuperAdmin) — que precisam enxergar exatamente a mesma lista.
    /// Antes cada lado aplicava o seu próprio fallback e eles divergiam com a seção ausente.
    /// </summary>
    public static class SuperAdminAllowlist
    {
        public const string SecaoConfiguracao = "SuperAdmins";

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
            !string.IsNullOrEmpty(userName)
            && Resolver(configuration).Contains(userName, StringComparer.OrdinalIgnoreCase);
    }
}
