using ApiControlePerifericos.Services;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace ApiControlePerifericos.Tests.Services
{
    public class TokenServiceTests
    {
        private static TokenService CriarService()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JWT:SecretKey"] = "uma-chave-de-teste-bem-grande-pra-passar-de-256bits!!",
                    ["JWT:TokenValidityInMinutes"] = "30",
                    ["JWT:ValidIssuer"] = "TesteIssuer",
                    ["JWT:ValidAudience"] = "TesteAudience",
                })
                .Build();

            return new TokenService(config);    
        }

        [Fact]
        public void GenerateRefreshToken_DeveRetornarStringNaoVazia()
        {
            // Arrange
            var service = CriarService();

            //Act
            var refreshToken = service.GenerateRefreshToken();

            //Assert
            Assert.False(string.IsNullOrEmpty(refreshToken));
        }

        [Fact]
        public void GenerateRefreshToken_DeveCodificarPara128Bytes()
        {
            // Arrange
            var service = CriarService();

            // Act
            var refreshToken = service.GenerateRefreshToken();
            var bytes = Convert.FromBase64String(refreshToken);

            // Assert
            Assert.Equal(128, bytes.Length);
        }

        [Fact]
        public void GenerateRefreshToken_DeveGerarTokensDiferentesACadaChamada()
        {
            // Arrange
            var service = CriarService();

            // Act
            var primeiro = service.GenerateRefreshToken();
            var segundo = service.GenerateRefreshToken();

            // Assert
            Assert.NotEqual(primeiro, segundo);
        }

        [Fact]
        public void GenerateToken_DeveConterOsClaimsInformados()
        {
            //arrange
            var service = CriarService();
            var claims = new List<Claim>
            {
                new Claim("id", "lucas.ody")
            };

            //Act
            var token = service.GenerateToken(claims);

            //Assert
            Assert.Contains(token.Claims, c => c.Type == "id" && c.Value == "lucas.ody");
        }

        [Fact]
        public void GenerateToken_DeveDefinirIssuerEAudienceDaConfiguracao()
        {
            //arrange
            var service = CriarService();

            //Act
            var token = service.GenerateToken(new List<Claim>());  

            //Assert
            Assert.Equal("TesteIssuer", token.Issuer);
            Assert.Contains("TesteAudience", token.Audiences);
        }

        [Fact]
        public void GenerateToken_SemSecretKey_DeveLancarInvalidOperationException()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JWT:TokenValidityInMinutes"] = "30",
                })
                .Build();

            var service = new TokenService(config);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => service.GenerateToken(new List<Claim>()));

        }

        [Fact]
        public void GetPrincipalFromExpiredToken_DeveExtrairOsClaimsDoTokenValido()
        {
            //arrange

            var service = CriarService();
            var claims = new List<Claim>
            {
                new Claim("id", "lucas.ody")
            };

            //gera o token e serializa para string(formato que chega a requisição)
            var jwt = service.GenerateToken(claims);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(jwt);

            //Act
            var principal = service.GetPrincipalFromExpiredToken(tokenString);

            //assert
            Assert.Contains(principal.Claims, c => c.Type == "id" && c.Value == "lucas.ody");
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_ComChaveDiferente_DeveLancarExcecao()
        {
            // Arrange — service A gera o token com a chave padrão
            var serviceQueGerou = CriarService();
            var jwt = serviceQueGerou.GenerateToken(new List<Claim>());
            var tokenString = new JwtSecurityTokenHandler().WriteToken(jwt);

            //Service B valida com outra Secrete Key
            var configoutraChave = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JWT:SecretKey"] = "uma-chave-COMPLETAMENTE-diferente-pra-quebrar-a-assinatura!!",
                })
                .Build();

            var serviceQueValida = new TokenService(configoutraChave);

            // Act & Assert
            Assert.ThrowsAny<SecurityTokenException>(() => serviceQueValida.GetPrincipalFromExpiredToken(tokenString));
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_SemSecretKey_DeveLancarInvalidOperationException()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            var service = new TokenService(config);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => service.GetPrincipalFromExpiredToken("qualquer-token"));
        }

    }
}

