using ApiControlePerifericos.Controllers;
using ApiControlePerifericos.DTOs.Identity;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration.Memory;

namespace ApiControlePerifericos.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<ITokenService> _tokenService = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<RoleManager<IdentityRole>> _roleManager;
        private readonly Mock<IConfiguration> _configuration = new();
        private readonly Mock<ILogger<AuthController>> _logger = new();
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _userManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            _roleManager = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);

            _controller = new AuthController(
                _tokenService.Object,
                _userManager.Object,
                _roleManager.Object,
                _configuration.Object,
                _logger.Object);
        }

        // ─── Helpers de Arrange ──────────────────────────────────────────────────

        private void ConfigurarUsuarioLogado(string userName, bool ehSuperAdmin = false)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim("id", userName)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var superAdmins = ehSuperAdmin ? new[] { userName } : Array.Empty<string>();
            ConfigurarSuperAdmins(superAdmins);
        }

        private void ConfigurarSuperAdmins(string[] userNames)
        {
            // Get<string[]>() é extension method — não pode ser mockado diretamente.
            // Construímos uma IConfiguration real e extraímos a seção dela.
            var dict = userNames
                .Select((u, i) => new KeyValuePair<string, string?>($"SuperAdmins:{i}", u))
                .ToDictionary(k => k.Key, v => v.Value);

            var realConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();

            _configuration
                .Setup(c => c.GetSection("SuperAdmins"))
                .Returns(realConfig.GetSection("SuperAdmins"));
        }

        private void ConfigurarConfig(string chave, string valor) =>
            _configuration.Setup(c => c[chave]).Returns(valor);

        // ─── Login ────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Login_UsuarioNaoEncontrado_DeveRetornar401()
        {
            // Arrange
            _userManager.Setup(u => u.FindByNameAsync("inexistente")).ReturnsAsync((ApplicationUser?)null);
            var model = new LoginRequest { UserName = "inexistente", Password = "123" };

            // Act
            var result = await _controller.Login(model);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Login_SenhaErrada_DeveRetornar401()
        {
            // Arrange
            var user = new ApplicationUser { UserName = "lucas" };
            _userManager.Setup(u => u.FindByNameAsync("lucas")).ReturnsAsync(user);
            _userManager.Setup(u => u.CheckPasswordAsync(user, "errada")).ReturnsAsync(false);
            var model = new LoginRequest { UserName = "lucas", Password = "errada" };

            // Act
            var result = await _controller.Login(model);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Login_CredenciaisValidas_DeveRetornar200ComTokenERefreshToken()
        {
            // Arrange
            var user = new ApplicationUser { UserName = "lucas", Email = "lucas@test.com" };
            _userManager.Setup(u => u.FindByNameAsync("lucas")).ReturnsAsync(user);
            _userManager.Setup(u => u.CheckPasswordAsync(user, "senha123")).ReturnsAsync(true);
            _userManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });
            _userManager.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _tokenService.Setup(t => t.GenerateToken(It.IsAny<List<Claim>>())).Returns(new JwtSecurityToken());
            _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token-gerado");
            ConfigurarConfig("JWT:RefreshTokenValidityInMinutes", "60");
            var model = new LoginRequest { UserName = "lucas", Password = "senha123" };

            // Act
            var result = await _controller.Login(model);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        // ─── Register ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task Register_UsuarioJaExiste_DeveRetornar409Conflict()
        {
            // Arrange
            var user = new ApplicationUser { UserName = "lucas" };
            _userManager.Setup(u => u.FindByNameAsync("lucas")).ReturnsAsync(user);
            var model = new RegisterRequest { UserName = "lucas", Password = "Senha@123", EmailAddress = "lucas@test.com" };

            // Act
            var result = await _controller.Register(model);

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Register_FalhaAoCriarUsuario_DeveRetornar500()
        {
            // Arrange
            _userManager.Setup(u => u.FindByNameAsync("novo")).ReturnsAsync((ApplicationUser?)null);
            _userManager
                .Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), "Senha@123"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Erro ao criar." }));
            var model = new RegisterRequest { UserName = "novo", Password = "Senha@123", EmailAddress = "novo@test.com" };

            // Act
            var result = await _controller.Register(model);

            // Assert
            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, status.StatusCode);
        }

        [Fact]
        public async Task Register_UsuarioNovoValido_DeveRetornar200()
        {
            // Arrange
            _userManager.Setup(u => u.FindByNameAsync("novo")).ReturnsAsync((ApplicationUser?)null);
            _userManager
                .Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), "Senha@123"))
                .ReturnsAsync(IdentityResult.Success);
            _userManager
                .Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
                .ReturnsAsync(IdentityResult.Success);
            var model = new RegisterRequest { UserName = "novo", Password = "Senha@123", EmailAddress = "novo@test.com" };

            // Act
            var result = await _controller.Register(model);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        // ─── CreateRole ───────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateRole_NomeEmBranco_DeveRetornar400()
        {
            // Arrange — string vazia passa o parâmetro mas falha na validação do controller
            // Act
            var result = await _controller.CreateRole("   ");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateRole_RoleJaExiste_DeveRetornar409Conflict()
        {
            // Arrange
            _roleManager.Setup(r => r.RoleExistsAsync("Admin")).ReturnsAsync(true);

            // Act
            var result = await _controller.CreateRole("Admin");

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task CreateRole_FalhaNaCriacao_DeveRetornar400()
        {
            // Arrange
            _roleManager.Setup(r => r.RoleExistsAsync("NovaRole")).ReturnsAsync(false);
            _roleManager
                .Setup(r => r.CreateAsync(It.IsAny<IdentityRole>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Erro." }));

            // Act
            var result = await _controller.CreateRole("NovaRole");

            // Assert
            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, status.StatusCode);
        }

        [Fact]
        public async Task CreateRole_RoleValida_DeveRetornar200()
        {
            // Arrange
            _roleManager.Setup(r => r.RoleExistsAsync("NovaRole")).ReturnsAsync(false);
            _roleManager
                .Setup(r => r.CreateAsync(It.IsAny<IdentityRole>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.CreateRole("NovaRole");

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        // ─── AddUserToRole ────────────────────────────────────────────────────────

        [Fact]
        public async Task AddUserToRole_UsuarioNaoEncontrado_DeveRetornar400()
        {
            // Arrange
            _userManager.Setup(u => u.FindByEmailAsync("naoexiste@test.com")).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _controller.AddUserToRole("naoexiste@test.com", "Admin");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddUserToRole_Sucesso_DeveRetornar200()
        {
            // Arrange
            var user = new ApplicationUser { UserName = "lucas", Email = "lucas@test.com" };
            _userManager.Setup(u => u.FindByEmailAsync("lucas@test.com")).ReturnsAsync(user);
            _userManager.Setup(u => u.AddToRoleAsync(user, "Admin")).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.AddUserToRole("lucas@test.com", "Admin");

            // Assert
            var ok = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        }

        // ─── ChangePassword ───────────────────────────────────────────────────────

        [Fact]
        public async Task ChangePassword_UsuarioNaoEncontrado_DeveRetornar404()
        {
            // Arrange
            ConfigurarUsuarioLogado("lucas");
            _userManager.Setup(u => u.FindByNameAsync("lucas")).ReturnsAsync((ApplicationUser?)null);
            var model = new ChangePasswordRequest { CurrentPassword = "Atual@123", NewPassword = "Nova@123" };

            // Act
            var result = await _controller.ChangePassword(model);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ChangePassword_FalhaAoTrocarSenha_DeveRetornar400()
        {
            // Arrange
            ConfigurarUsuarioLogado("lucas");
            var user = new ApplicationUser { UserName = "lucas" };
            _userManager.Setup(u => u.FindByNameAsync("lucas")).ReturnsAsync(user);
            _userManager
                .Setup(u => u.ChangePasswordAsync(user, "Errada@123", "Nova@123"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Senha atual incorreta." }));
            var model = new ChangePasswordRequest { CurrentPassword = "Errada@123", NewPassword = "Nova@123" };

            // Act
            var result = await _controller.ChangePassword(model);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ChangePassword_Sucesso_DeveRetornar200()
        {
            // Arrange
            ConfigurarUsuarioLogado("lucas");
            var user = new ApplicationUser { UserName = "lucas" };
            _userManager.Setup(u => u.FindByNameAsync("lucas")).ReturnsAsync(user);
            _userManager
                .Setup(u => u.ChangePasswordAsync(user, "Atual@123", "Nova@123"))
                .ReturnsAsync(IdentityResult.Success);
            var model = new ChangePasswordRequest { CurrentPassword = "Atual@123", NewPassword = "Nova@123" };

            // Act
            var result = await _controller.ChangePassword(model);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        // ─── ResetPassword ────────────────────────────────────────────────────────

        [Fact]
        public async Task ResetPassword_UsuarioNaoEncontrado_DeveRetornar404()
        {
            // Arrange
            ConfigurarUsuarioLogado("admin");
            _userManager.Setup(u => u.FindByNameAsync("inexistente")).ReturnsAsync((ApplicationUser?)null);
            var model = new AdminResetPasswordRequest { UserName = "inexistente", NewPassword = "Nova@123" };

            // Act
            var result = await _controller.ResetPassword(model);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ResetPassword_AdminTentaResetarSuperAdmin_DeveRetornar403Forbid()
        {
            // Arrange — solicitante é admin comum; alvo é super admin
            ConfigurarUsuarioLogado("admin.comum", ehSuperAdmin: false);
            ConfigurarSuperAdmins(new[] { "lucas.ody" });

            var alvo = new ApplicationUser { UserName = "lucas.ody" };
            _userManager.Setup(u => u.FindByNameAsync("lucas.ody")).ReturnsAsync(alvo);
            var model = new AdminResetPasswordRequest { UserName = "lucas.ody", NewPassword = "Nova@123" };

            // Act
            var result = await _controller.ResetPassword(model);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task ResetPassword_Sucesso_DeveRetornar200()
        {
            // Arrange — solicitante é super admin resetando outro usuário comum
            ConfigurarUsuarioLogado("lucas.ody", ehSuperAdmin: true);
            var alvo = new ApplicationUser { UserName = "outro.usuario" };
            _userManager.Setup(u => u.FindByNameAsync("outro.usuario")).ReturnsAsync(alvo);
            _userManager
                .Setup(u => u.GeneratePasswordResetTokenAsync(alvo))
                .ReturnsAsync("reset-token");
            _userManager
                .Setup(u => u.ResetPasswordAsync(alvo, "reset-token", "Nova@123"))
                .ReturnsAsync(IdentityResult.Success);
            var model = new AdminResetPasswordRequest { UserName = "outro.usuario", NewPassword = "Nova@123" };

            // Act
            var result = await _controller.ResetPassword(model);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        // ─── RefreshToken ─────────────────────────────────────────────────────────

        [Fact]
        public async Task RefreshToken_ModelNulo_DeveRetornar400()
        {
            // Arrange
            // Act
            var result = await _controller.RefreshToken(null!);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RefreshToken_PrincipalInvalido_DeveRetornar400()
        {
            // Arrange
            _tokenService
                .Setup(t => t.GetPrincipalFromExpiredToken("token-invalido"))
                .Returns((ClaimsPrincipal?)null);
            var model = new TokenResponse { AccessToken = "token-invalido", RefreshToken = "qualquer" };

            // Act
            var result = await _controller.RefreshToken(model);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RefreshToken_UsuarioNaoEncontradoOuTokenExpirado_DeveRetornar400()
        {
            // Arrange
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "lucas") }, "Test");
            var principal = new ClaimsPrincipal(identity);
            _tokenService
                .Setup(t => t.GetPrincipalFromExpiredToken("token-expirado"))
                .Returns(principal);
            _userManager.Setup(u => u.FindByNameAsync("lucas")).ReturnsAsync((ApplicationUser?)null);
            var model = new TokenResponse { AccessToken = "token-expirado", RefreshToken = "refresh-antigo" };

            // Act
            var result = await _controller.RefreshToken(model);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RefreshToken_Sucesso_DeveRetornar200ComNovosTokens()
        {
            // Arrange
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "lucas") }, "Test");
            var principal = new ClaimsPrincipal(identity);
            var user = new ApplicationUser
            {
                UserName = "lucas",
                RefreshToken = "refresh-valido",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(30)
            };
            _tokenService
                .Setup(t => t.GetPrincipalFromExpiredToken("token-expirado"))
                .Returns(principal);
            _userManager.Setup(u => u.FindByNameAsync("lucas")).ReturnsAsync(user);
            _userManager.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _tokenService.Setup(t => t.GenerateToken(It.IsAny<List<Claim>>())).Returns(new JwtSecurityToken());
            _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("novo-refresh-token");
            ConfigurarConfig("JWT:RefreshTokenValidityInMinutes", "60");
            var model = new TokenResponse { AccessToken = "token-expirado", RefreshToken = "refresh-valido" };

            // Act
            var result = await _controller.RefreshToken(model);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        // ─── Revoke ───────────────────────────────────────────────────────────────

        [Fact]
        public async Task Revoke_UsernameDiferenteDoLogado_DeveRetornar403Forbid()
        {
            // Arrange
            ConfigurarUsuarioLogado("lucas");

            // Act — tenta revogar outro usuário
            var result = await _controller.Revoke("outro.usuario");

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Revoke_UsuarioNaoEncontrado_DeveRetornar404()
        {
            // Arrange
            ConfigurarUsuarioLogado("lucas");
            _userManager.Setup(u => u.FindByNameAsync("lucas")).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _controller.Revoke("lucas");

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Revoke_Sucesso_DeveRetornar204()
        {
            // Arrange
            ConfigurarUsuarioLogado("lucas");
            var user = new ApplicationUser { UserName = "lucas", RefreshToken = "token-ativo" };
            _userManager.Setup(u => u.FindByNameAsync("lucas")).ReturnsAsync(user);
            _userManager.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.Revoke("lucas");

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(user.RefreshToken);
        }
    }
}
