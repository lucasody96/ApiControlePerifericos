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

        /// <summary>
        /// Testa se o método GenerateRefreshToken retorna uma string não vazia.
        /// </summary>
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

        /// <summary>
        /// Testa se o método GenerateRefreshToken retorna uma string que, quando decodificada de Base64, tem exatamente 128 bytes.
        /// </summary>
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

        /// <summary>
        /// Testa se o método GenerateRefreshToken retorna tokens diferentes a cada chamada.
        /// </summary>
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

        /// <summary>
        /// Testa se o método GenerateToken inclui os claims fornecidos no token gerado.
        /// </summary>
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

        /// <summary>
        /// Testa se o método GenerateToken define corretamente o issuer e audience do token com base na configuração fornecida.
        /// </summary>
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

        /// <summary>
        /// Testa se o método GenerateToken lança uma InvalidOperationException quando a chave secreta não está presente na configuração.
        /// </summary>
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

        /// <summary>
        /// Testa se o método GetPrincipalFromExpiredToken consegue extrair corretamente os claims de um token válido, mesmo que esteja expirado.
        /// </summary>
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

        /// <summary>
        /// Testa se o método GetPrincipalFromExpiredToken lança uma SecurityTokenException quando o token fornecido foi assinado com uma chave diferente da esperada.
        /// </summary>
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

        /// <summary>
        /// Testa se o método GetPrincipalFromExpiredToken lança uma InvalidOperationException quando a chave secreta não está presente na configuração.
        /// </summary>
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

