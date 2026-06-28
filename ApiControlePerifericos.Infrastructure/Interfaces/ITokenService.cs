using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ApiControlePerifericos.Interfaces
{
    public interface ITokenService
    {
        JwtSecurityToken GenerateToken(IEnumerable<Claim> claims);

        string GenerateRefreshToken();

        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
