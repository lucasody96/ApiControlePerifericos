using ApiControlePerifericos.DTOs.Identity;
using ApiControlePerifericos.Extensions;
using ApiControlePerifericos.Models.Identity;
using ApiControlePerifericos.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;
using X.PagedList.Extensions;

namespace ApiControlePerifericos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class UsuariosController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(UserManager<ApplicationUser> userManager,
                                  RoleManager<IdentityRole> roleManager,
                                  ILogger<UsuariosController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [HttpGet("pagination")]
        public async Task<ActionResult<IEnumerable<UsuarioResponse>>> Get([FromQuery] UsuariosParameters parameters)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(parameters.Busca))
            {
                // Identity guarda as versões normalizadas (em maiúsculas), então
                // comparar com o termo em maiúsculas dá busca case-insensitive
                // sem depender do collation da coluna (gotcha do TiDB).
                var termo = parameters.Busca.ToUpperInvariant();
                query = query.Where(u =>
                    (u.NormalizedUserName != null && u.NormalizedUserName.Contains(termo)) ||
                    (u.NormalizedEmail != null && u.NormalizedEmail.Contains(termo)));
            }

            var usuariosPaginados = query.OrderBy(u => u.UserName).ToPagedList(parameters.PageNumber, parameters.PageSize);

            Response.AdicionarHeaderDePaginacao(usuariosPaginados);

            return Ok(await MapearUsuariosAsync(usuariosPaginados));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioResponse>>> Get()
        {
            var usuarios = _userManager.Users.OrderBy(u => u.UserName).ToList();
            return Ok(await MapearUsuariosAsync(usuarios));
        }

        [HttpGet("roles")]
        public ActionResult<IEnumerable<string>> GetRoles()
        {
            var roles = _roleManager.Roles
                .Where(r => r.Name != null)
                .Select(r => r.Name!)
                .OrderBy(nome => nome)
                .ToList();

            return Ok(roles);
        }

        // Alterar roles é mais sensível que o resto da gestão: restrito a
        // SuperAdmin (sobrepõe o AdminOnly do controller).
        [HttpPut("{userName}/roles")]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> AtualizarRoles(string userName, [FromBody] AtualizarRolesRequest model)
        {
            var user = await _userManager.FindByNameAsync(userName);

            if (user == null)
                return NotFound(new Response { Status = "Error", Message = "Usuário não encontrado." });

            // Valida que todas as roles informadas existem antes de mexer no usuário.
            foreach (var role in model.Roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                    return BadRequest(new Response { Status = "Error", Message = $"A role '{role}' não existe." });
            }

            var rolesAtuais = await _userManager.GetRolesAsync(user);
            var aRemover = rolesAtuais.Except(model.Roles).ToList();
            var aAdicionar = model.Roles.Except(rolesAtuais).ToList();

            if (aRemover.Count > 0)
            {
                var resultadoRemocao = await _userManager.RemoveFromRolesAsync(user, aRemover);
                if (!resultadoRemocao.Succeeded)
                    return BadRequest(new Response { Status = "Error", Message = "Falha ao remover roles do usuário." });
            }

            if (aAdicionar.Count > 0)
            {
                var resultadoInclusao = await _userManager.AddToRolesAsync(user, aAdicionar);
                if (!resultadoInclusao.Succeeded)
                    return BadRequest(new Response { Status = "Error", Message = "Falha ao adicionar roles ao usuário." });
            }

            _logger.LogInformation("Roles do usuário {UserName} atualizadas para: {Roles}.",
                userName, string.Join(", ", model.Roles));

            return Ok(new Response { Status = "Success", Message = "Roles do usuário atualizadas com sucesso." });
        }

        // Carrega as roles de cada usuário (uma consulta por usuário — aceitável
        // para o volume desta aplicação interna).
        private async Task<List<UsuarioResponse>> MapearUsuariosAsync(IEnumerable<ApplicationUser> usuarios)
        {
            var resposta = new List<UsuarioResponse>();

            foreach (var usuario in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(usuario);
                resposta.Add(new UsuarioResponse
                {
                    Id = usuario.Id,
                    UserName = usuario.UserName,
                    Email = usuario.Email,
                    Roles = roles.ToArray()
                });
            }

            return resposta;
        }
    }
}
