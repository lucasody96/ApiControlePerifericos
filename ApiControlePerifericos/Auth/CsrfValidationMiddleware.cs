using System.Security.Cryptography;
using System.Text;

namespace ApiControlePerifericos.Auth
{
    // Proteção CSRF por double-submit cookie: em requisições que alteram estado
    // (POST/PUT/DELETE/PATCH), exige que o header X-CSRF-Token seja igual ao cookie
    // XSRF-TOKEN. Como o cookie é setado por nós e um site malicioso não consegue
    // lê-lo (mesmo não sendo httpOnly, a Same-Origin Policy impede a leitura cross-site),
    // ele não consegue reproduzir o header — e o request é barrado.
    //
    // Necessário porque os cookies de auth são SameSite=None (cenário cross-site
    // Vercel↔Cloud Run), então o SameSite sozinho não protege contra CSRF.
    public class CsrfValidationMiddleware
    {
        private readonly RequestDelegate _next;

        private static readonly HashSet<string> MetodosSeguros =
            new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD", "OPTIONS", "TRACE" };

        // O login é isento: quando o usuário autentica ainda não existe cookie CSRF.
        private static readonly PathString CaminhoLogin = new("/api/Auth/login");

        public CsrfValidationMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;

            var precisaValidar =
                !MetodosSeguros.Contains(request.Method) &&
                !request.Path.StartsWithSegments(CaminhoLogin, StringComparison.OrdinalIgnoreCase);

            if (precisaValidar && !CsrfValido(request))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(
                    new { message = "Token anti-CSRF ausente ou inválido." });
                return;
            }

            await _next(context);
        }

        private static bool CsrfValido(HttpRequest request)
        {
            var cookieToken = request.Cookies[AuthCookies.Csrf];
            var headerToken = request.Headers[AuthCookies.CsrfHeader].ToString();

            if (string.IsNullOrEmpty(cookieToken) || string.IsNullOrEmpty(headerToken))
                return false;

            // Comparação em tempo constante para não vazar informação por timing.
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(cookieToken),
                Encoding.UTF8.GetBytes(headerToken));
        }
    }
}
