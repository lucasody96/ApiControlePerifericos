namespace ApiControlePerifericos.Auth
{
    // Nomes dos cookies usados no fluxo de autenticação por cookie httpOnly.
    // Centralizados aqui porque são lidos em mais de um lugar (AuthController grava,
    // Program.cs lê o access token no JwtBearer, CsrfValidationMiddleware compara o CSRF).
    public static class AuthCookies
    {
        public const string AccessToken = "accessToken";
        public const string RefreshToken = "refreshToken";

        // Cookie do token anti-CSRF (double-submit). NÃO é httpOnly — o JS precisa
        // lê-lo para reenviá-lo no header. O nome segue o padrão que o axios usa por
        // convenção (xsrfCookieName/xsrfHeaderName).
        public const string Csrf = "XSRF-TOKEN";
        public const string CsrfHeader = "X-CSRF-Token";
    }
}
