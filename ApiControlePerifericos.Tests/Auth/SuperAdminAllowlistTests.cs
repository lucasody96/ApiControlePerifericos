using System.Security.Claims;
using ApiControlePerifericos.Auth;
using Microsoft.Extensions.Configuration;

namespace ApiControlePerifericos.Tests.Auth
{
    // A allowlist de super admins é consumida por dois caminhos com regras historicamente
    // diferentes (issue #26): a policy "SuperAdminOnly" (via claim "id") e o
    // AuthController.EhSuperAdmin (via username). Estes testes fixam que os dois usam o
    // mesmo critério de comparação — case-insensitive, como o Identity trata username.
    public class SuperAdminAllowlistTests
    {
        private static IConfiguration ConfigurarAllowlist(params string[] superAdmins)
        {
            var valores = superAdmins
                .Select((nome, indice) => new KeyValuePair<string, string?>(
                    $"{SuperAdminAllowlist.SecaoConfiguracao}:{indice}", nome));

            return new ConfigurationBuilder()
                .AddInMemoryCollection(valores)
                .Build();
        }

        private static ClaimsPrincipal PrincipalCom(string userName) =>
            new(new ClaimsIdentity([new Claim(SuperAdminAllowlist.TipoClaimUserName, userName)], "TestAuth"));

        [Fact]
        public void Contem_ConfigComCaixaDivergenteDoUserName_RetornaTrue()
        {
            // Arrange — config grafada diferente do UserName persistido no Identity
            var configuration = ConfigurarAllowlist("Lucas.Ody");

            // Act
            var resultado = SuperAdminAllowlist.Contem(configuration, "lucas.ody");

            // Assert
            Assert.True(resultado);
        }

        [Fact]
        public void EhSuperAdmin_ClaimComCaixaDivergenteDaConfig_RetornaTrue()
        {
            // Arrange — mesmo cenário do teste acima, mas pelo caminho da policy
            var allowlist = SuperAdminAllowlist.Resolver(ConfigurarAllowlist("Lucas.Ody"));
            var principal = PrincipalCom("lucas.ody");

            // Act
            var resultado = SuperAdminAllowlist.EhSuperAdmin(principal, allowlist);

            // Assert
            Assert.True(resultado);
        }

        [Fact]
        public void PolicyEControllerConcordam_ParaOMesmoUserName()
        {
            // Arrange
            var configuration = ConfigurarAllowlist("Lucas.Ody");
            var allowlist = SuperAdminAllowlist.Resolver(configuration);
            const string userName = "LUCAS.ODY";

            // Act
            var pelaPolicy = SuperAdminAllowlist.EhSuperAdmin(PrincipalCom(userName), allowlist);
            var peloController = SuperAdminAllowlist.Contem(configuration, userName);

            // Assert
            Assert.Equal(pelaPolicy, peloController);
            Assert.True(pelaPolicy);
        }

        [Fact]
        public void EhSuperAdmin_UsuarioForaDaAllowlist_RetornaFalse()
        {
            // Arrange
            var allowlist = SuperAdminAllowlist.Resolver(ConfigurarAllowlist("Lucas.Ody"));

            // Act
            var resultado = SuperAdminAllowlist.EhSuperAdmin(PrincipalCom("outro.usuario"), allowlist);

            // Assert
            Assert.False(resultado);
        }

        [Fact]
        public void EhSuperAdmin_PrincipalSemClaimDeUserName_RetornaFalse()
        {
            // Arrange
            var allowlist = SuperAdminAllowlist.Resolver(ConfigurarAllowlist("Lucas.Ody"));
            var principal = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var resultado = SuperAdminAllowlist.EhSuperAdmin(principal, allowlist);

            // Assert
            Assert.False(resultado);
        }

        [Fact]
        public void Resolver_SecaoAusente_UsaFallback()
        {
            // Arrange — seção "SuperAdmins" ausente
            var configuration = new ConfigurationBuilder().Build();

            // Act
            var allowlist = SuperAdminAllowlist.Resolver(configuration);

            // Assert — o fallback continua valendo e também é case-insensitive
            Assert.Contains("lucas.ody", allowlist);
            Assert.True(SuperAdminAllowlist.Contem(allowlist, "Admin"));
        }
    }
}
