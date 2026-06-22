using ApiControlePerifericos.DTOs.Identity;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ApiControlePerifericos.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ITokenService tokenService,
                              UserManager<ApplicationUser> userManager,
                              RoleManager<IdentityRole> roleManager,
                              IConfiguration configuration,
                              ILogger<AuthController> logger)
        {
            _tokenService = tokenService;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _logger = logger;
        }

        [Authorize(Policy = "SuperAdminOnly")]
        [HttpPost]
        [Route("CreateRole")]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return BadRequest(new Response { Status = "Error", Message = "O nome da role é obrigatório." });

            if (await _roleManager.RoleExistsAsync(roleName))
                return Conflict(new Response { Status = "Error", Message = "A role já existe!" });

            var roleResult = await _roleManager.CreateAsync(new IdentityRole(roleName));

            if (!roleResult.Succeeded)
            {
                _logger.LogError("Falha ao criar a role {RoleName}: {Errors}",
                    roleName, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                return StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "Error", Message = "Falha ao criar a role!" });
            }

            _logger.LogInformation("Role {RoleName} criada com sucesso.", roleName);
            return Ok(new Response { Status = "Success", Message = $"A role {roleName} foi criada com sucesso." });
        }

        [HttpPost]
        [Route("AddUserToRole")]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> AddUserToRole(string email, string roleName)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                var result = await _userManager.AddToRoleAsync(user, roleName);

                if (result.Succeeded)
                {
                    _logger.LogInformation(1, $"User {user.Email} added to role {roleName} successfully.");
                    return StatusCode(StatusCodes.Status200OK, new Response { Status = "Success", Message = $"User {user.Email} added to role {roleName} successfully." });
                }
                else
                {
                    _logger.LogInformation(1, $"Failed to add user {user.Email} to role {roleName}");
                    return StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "Error", Message = $"Failed to add user {user.Email} to role {roleName}." });
                }
            }
            return BadRequest(new { error = "Unable to find user." });
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var user = await _userManager.FindByNameAsync(model.UserName!);

            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password!))
                return Unauthorized(new { message = "Usuário ou senha inválidos" });

            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim("id", user.UserName!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var token = _tokenService.GenerateToken(authClaims);

            var refreshToken = _tokenService.GenerateRefreshToken();

            if (!int.TryParse(_configuration["JWT:RefreshTokenValidityInMinutes"], out int refreshTokenValidityInMinutes))
                throw new InvalidOperationException("JWT:RefreshTokenValidityInMinutes não está configurado corretamente.");

            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(refreshTokenValidityInMinutes);

            user.RefreshToken = refreshToken;

            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = token.ValidTo,
                RefreshToken = refreshToken
            });
        }

        [HttpPost]
        [Route("Register")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            var userExists = await _userManager.FindByNameAsync(model.UserName!);

            if (userExists != null)
                return Conflict(new Response { Status = "Error", Message = "Usuário já existe!" });

            ApplicationUser user = new()
            {
                Email = model.EmailAddress,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.UserName
            };

            var result = await _userManager.CreateAsync(user, model.Password!);

            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "Falha ao criar usuário!" });

            await _userManager.AddToRoleAsync(user, "User");

            return Ok(new { message = "Usuário criado com sucesso!" });

        }

        [HttpPost]
        [Route("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name!);

            if (user == null)
                return NotFound(new Response { Status = "Error", Message = "Usuário não encontrado" });

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword!, model.NewPassword!);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Falha ao alterar a senha para o usuário {UserName}: {Errors}",
                    user.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(new Response { Status = "Error", Message = "Falha ao alterar a senha. Verifique a senha atual e os requisitos da nova senha." });
            }
            
            _logger.LogInformation("Senha alterada com sucesso para o usuário {UserName}.", user.UserName);
            return Ok(new { message = "Senha alterada com sucesso!" });
        }

        [HttpPost]
        [Route("ResetPassword")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ResetPassword([FromBody] AdminResetPasswordRequest model)
        {
            var user = await _userManager.FindByNameAsync(model.UserName!);

            if (user == null)
                return NotFound(new Response { Status = "Error", Message = "Usuário não encontrado" });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword!);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Falha ao redefinir a senha para o usuário {UserName}: {Errors}",
                    user.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(new Response { Status = "Error", Message = "Falha ao redefinir a senha. Verifique o token e os requisitos da nova senha." });
            }

            _logger.LogInformation("Senha do usuário {UserName} resetada por {Admin}.", user.UserName, User.Identity!.Name);
            return Ok(new Response { Status = "Success", Message = $"Senha do usuário {user.UserName} resetada com sucesso." });
        }

        [HttpPost]
        [Route("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenResponse model)
        {
            if (model is null)
                return BadRequest("Requisição inválida");

            string? accessToken = model.AccessToken ?? throw new ArgumentNullException(nameof(model));

            string? refreshToken = model.RefreshToken ?? throw new ArgumentNullException(nameof(model));

            var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken!);

            if (principal is null)
                return BadRequest("Access token ou refresh token inválido");

            var username = principal.Identity!.Name;

            var user = await _userManager.FindByNameAsync(username!);

            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return BadRequest("Requisição inválida");

            var newAccessToken = _tokenService.GenerateToken(principal.Claims.ToList());

            var newRefreshToken = _tokenService.GenerateRefreshToken();

            if (!int.TryParse(_configuration["JWT:RefreshTokenValidityInMinutes"], out int refreshTokenValidityInMinutes))
                throw new InvalidOperationException("JWT:RefreshTokenValidityInMinutes não está configurado corretamente.");

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(refreshTokenValidityInMinutes);
            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                accessToken = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
                refreshToken = newRefreshToken
            });

        }

        [Authorize(Policy = "SuperAdminOnly")]
        [HttpPost]
        [Route("revoke/{username}")]
        public async Task<IActionResult> Revoke(string username)
        {
            if (!string.Equals(User.Identity?.Name, username, StringComparison.Ordinal))
                return Forbid();

            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
                return NotFound("Usuário não encontrado");

            user.RefreshToken = null;

            await _userManager.UpdateAsync(user);

            return NoContent();

        }
    }
}
