using ApiControlePerifericos.Controllers;
using ApiControlePerifericos.DTOs.Identity;
using ApiControlePerifericos.Models.Identity;
using ApiControlePerifericos.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ApiControlePerifericos.Tests.Controllers
{
    // UsuariosController não usa IUnitOfWork nem AutoMapper: consome UserManager /
    // RoleManager direto, então os dois são mockados (Identity exige os construtores
    // com stores fake — mesmo padrão de AuthControllerTests).
    public class UsuariosControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<RoleManager<IdentityRole>> _roleManager;
        private readonly Mock<ILogger<UsuariosController>> _logger = new();
        private readonly UsuariosController _controller;

        public UsuariosControllerTests()
        {
            // Os null! são os parâmetros opcionais do construtor do Identity que estes
            // testes não exercitam (option accessors, validators, hashers...).
            _userManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null!, null!, null!, null!, null!, null!, null!, null!);

            _roleManager = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null!, null!, null!, null!);

            _controller = new UsuariosController(
                _userManager.Object,
                _roleManager.Object,
                _logger.Object);

            // O endpoint paginado escreve o header X-Pagination em Response.
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Por padrão nenhum usuário tem role; testes que precisam sobrescrevem.
            _userManager.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
                        .ReturnsAsync(new List<string>());
        }

        // ─── Helpers de Arrange ──────────────────────────────────────────────────

        private static ApplicationUser CriarUsuario(string userName, string email) => new()
        {
            Id = Guid.NewGuid().ToString(),
            UserName = userName,
            Email = email,
            // O Identity normaliza em maiúsculas; a busca do controller compara
            // contra estas colunas para não depender do collation do banco.
            NormalizedUserName = userName.ToUpperInvariant(),
            NormalizedEmail = email.ToUpperInvariant()
        };

        private void ConfigurarUsuarios(params ApplicationUser[] usuarios) =>
            _userManager.Setup(m => m.Users).Returns(usuarios.AsQueryable());

        // ─── GET /pagination ─────────────────────────────────────────────────────

        [Fact]
        public async Task GetPagination_QuandoExistemUsuarios_DeveRetornar200ComUsuariosOrdenados()
        {
            // Arrange
            ConfigurarUsuarios(
                CriarUsuario("bruno.costa", "bruno@empresa.com"),
                CriarUsuario("ana.silva", "ana@empresa.com"));

            // Act
            var result = await _controller.Get(new UsuariosParameters { PageNumber = 1, PageSize = 10 });

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var usuarios = Assert.IsAssignableFrom<IEnumerable<UsuarioResponse>>(ok.Value).ToList();
            Assert.Equal(2, usuarios.Count);
            Assert.Equal("ana.silva", usuarios[0].UserName);
            Assert.Equal("bruno.costa", usuarios[1].UserName);
        }

        [Fact]
        public async Task GetPagination_DeveEscreverHeaderDePaginacao()
        {
            // Arrange
            ConfigurarUsuarios(CriarUsuario("ana.silva", "ana@empresa.com"));

            // Act
            await _controller.Get(new UsuariosParameters { PageNumber = 1, PageSize = 10 });

            // Assert: o frontend depende deste header para montar a paginação.
            var header = _controller.Response.Headers["X-Pagination"].ToString();
            Assert.Contains("\"TotalItemCount\":1", header);
            Assert.Contains("\"HasNextPage\":false", header);
        }

        [Fact]
        public async Task GetPagination_ComBuscaEmCaixaDiferente_DeveEncontrarPorUserName()
        {
            // Arrange: busca em minúsculas contra NormalizedUserName em maiúsculas.
            ConfigurarUsuarios(
                CriarUsuario("Ana.Silva", "ana@empresa.com"),
                CriarUsuario("bruno.costa", "bruno@empresa.com"));

            // Act
            var result = await _controller.Get(new UsuariosParameters { Busca = "ana" });

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var usuarios = Assert.IsAssignableFrom<IEnumerable<UsuarioResponse>>(ok.Value).ToList();
            var unico = Assert.Single(usuarios);
            Assert.Equal("Ana.Silva", unico.UserName);
        }

        [Fact]
        public async Task GetPagination_ComBuscaPorEmail_DeveEncontrarIgnorandoCaixa()
        {
            // Arrange
            ConfigurarUsuarios(
                CriarUsuario("ana.silva", "Ana@Empresa.com"),
                CriarUsuario("bruno.costa", "bruno@empresa.com"));

            // Act
            var result = await _controller.Get(new UsuariosParameters { Busca = "ANA@EMPRESA" });

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var usuarios = Assert.IsAssignableFrom<IEnumerable<UsuarioResponse>>(ok.Value).ToList();
            var unico = Assert.Single(usuarios);
            Assert.Equal("ana.silva", unico.UserName);
        }

        [Fact]
        public async Task GetPagination_QuandoBuscaNaoCasa_DeveRetornar200ComListaVazia()
        {
            // Arrange
            ConfigurarUsuarios(CriarUsuario("ana.silva", "ana@empresa.com"));

            // Act
            var result = await _controller.Get(new UsuariosParameters { Busca = "inexistente" });

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var usuarios = Assert.IsAssignableFrom<IEnumerable<UsuarioResponse>>(ok.Value);
            Assert.Empty(usuarios);
        }

        // ─── GET (sem paginação) ─────────────────────────────────────────────────

        [Fact]
        public async Task Get_QuandoExistemUsuarios_DeveRetornar200ComRolesDeCadaUm()
        {
            // Arrange
            var ana = CriarUsuario("ana.silva", "ana@empresa.com");
            ConfigurarUsuarios(ana);
            _userManager.Setup(m => m.GetRolesAsync(ana)).ReturnsAsync(new List<string> { "Admin", "User" });

            // Act
            var result = await _controller.Get();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var usuarios = Assert.IsAssignableFrom<IEnumerable<UsuarioResponse>>(ok.Value).ToList();
            var unico = Assert.Single(usuarios);
            Assert.Equal("ana.silva", unico.UserName);
            Assert.Equal(["Admin", "User"], unico.Roles);
        }

        [Fact]
        public async Task Get_QuandoNaoHaUsuarios_DeveRetornar200ComListaVazia()
        {
            // Arrange
            ConfigurarUsuarios();

            // Act
            var result = await _controller.Get();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Empty(Assert.IsAssignableFrom<IEnumerable<UsuarioResponse>>(ok.Value));
        }

        // ─── GET /roles ──────────────────────────────────────────────────────────

        [Fact]
        public void GetRoles_DeveRetornar200ComRolesOrdenadas()
        {
            // Arrange
            _roleManager.Setup(m => m.Roles).Returns(new List<IdentityRole>
            {
                new("User"),
                new("Admin")
            }.AsQueryable());

            // Act
            var result = _controller.GetRoles();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var roles = Assert.IsAssignableFrom<IEnumerable<string>>(ok.Value);
            Assert.Equal(["Admin", "User"], roles);
        }

        // ─── PUT /{userName}/roles ───────────────────────────────────────────────

        [Fact]
        public async Task AtualizarRoles_QuandoUsuarioNaoExiste_DeveRetornar404()
        {
            // Arrange
            _userManager.Setup(m => m.FindByNameAsync("fantasma")).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _controller.AtualizarRoles("fantasma",
                new AtualizarRolesRequest { Roles = ["Admin"] });

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var resposta = Assert.IsType<Response>(notFound.Value);
            Assert.Equal("Error", resposta.Status);
            _userManager.Verify(m => m.AddToRolesAsync(It.IsAny<ApplicationUser>(),
                It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Fact]
        public async Task AtualizarRoles_QuandoRoleNaoExiste_DeveRetornar400ENaoAlterarUsuario()
        {
            // Arrange
            var ana = CriarUsuario("ana.silva", "ana@empresa.com");
            _userManager.Setup(m => m.FindByNameAsync("ana.silva")).ReturnsAsync(ana);
            _roleManager.Setup(m => m.RoleExistsAsync("Inexistente")).ReturnsAsync(false);

            // Act
            var result = await _controller.AtualizarRoles("ana.silva",
                new AtualizarRolesRequest { Roles = ["Inexistente"] });

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var resposta = Assert.IsType<Response>(badRequest.Value);
            Assert.Contains("Inexistente", resposta.Message);
            _userManager.Verify(m => m.AddToRolesAsync(It.IsAny<ApplicationUser>(),
                It.IsAny<IEnumerable<string>>()), Times.Never);
            _userManager.Verify(m => m.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(),
                It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Fact]
        public async Task AtualizarRoles_QuandoValido_DeveAdicionarERemoverApenasADiferenca()
        {
            // Arrange: usuário é "User" e deve passar a ser "Admin".
            var ana = CriarUsuario("ana.silva", "ana@empresa.com");
            _userManager.Setup(m => m.FindByNameAsync("ana.silva")).ReturnsAsync(ana);
            _userManager.Setup(m => m.GetRolesAsync(ana)).ReturnsAsync(new List<string> { "User" });
            _roleManager.Setup(m => m.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
            _userManager.Setup(m => m.RemoveFromRolesAsync(ana, It.IsAny<IEnumerable<string>>()))
                        .ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.AddToRolesAsync(ana, It.IsAny<IEnumerable<string>>()))
                        .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.AtualizarRoles("ana.silva",
                new AtualizarRolesRequest { Roles = ["Admin"] });

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Success", Assert.IsType<Response>(ok.Value).Status);
            _userManager.Verify(m => m.RemoveFromRolesAsync(ana,
                It.Is<IEnumerable<string>>(r => r.SequenceEqual(new[] { "User" }))), Times.Once);
            _userManager.Verify(m => m.AddToRolesAsync(ana,
                It.Is<IEnumerable<string>>(r => r.SequenceEqual(new[] { "Admin" }))), Times.Once);
        }

        [Fact]
        public async Task AtualizarRoles_QuandoRolesNaoMudam_NaoDeveChamarIdentity()
        {
            // Arrange: conjunto final igual ao atual — nada a adicionar nem remover.
            var ana = CriarUsuario("ana.silva", "ana@empresa.com");
            _userManager.Setup(m => m.FindByNameAsync("ana.silva")).ReturnsAsync(ana);
            _userManager.Setup(m => m.GetRolesAsync(ana)).ReturnsAsync(new List<string> { "Admin" });
            _roleManager.Setup(m => m.RoleExistsAsync("Admin")).ReturnsAsync(true);

            // Act
            var result = await _controller.AtualizarRoles("ana.silva",
                new AtualizarRolesRequest { Roles = ["Admin"] });

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _userManager.Verify(m => m.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(),
                It.IsAny<IEnumerable<string>>()), Times.Never);
            _userManager.Verify(m => m.AddToRolesAsync(It.IsAny<ApplicationUser>(),
                It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Fact]
        public async Task AtualizarRoles_QuandoIdentityFalhaAoAdicionar_DeveRetornar400()
        {
            // Arrange
            var ana = CriarUsuario("ana.silva", "ana@empresa.com");
            _userManager.Setup(m => m.FindByNameAsync("ana.silva")).ReturnsAsync(ana);
            _userManager.Setup(m => m.GetRolesAsync(ana)).ReturnsAsync(new List<string>());
            _roleManager.Setup(m => m.RoleExistsAsync("Admin")).ReturnsAsync(true);
            _userManager.Setup(m => m.AddToRolesAsync(ana, It.IsAny<IEnumerable<string>>()))
                        .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "falhou" }));

            // Act
            var result = await _controller.AtualizarRoles("ana.silva",
                new AtualizarRolesRequest { Roles = ["Admin"] });

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Error", Assert.IsType<Response>(badRequest.Value).Status);
        }
    }
}
