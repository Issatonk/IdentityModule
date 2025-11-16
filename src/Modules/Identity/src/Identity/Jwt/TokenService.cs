using Identity.Dto;
using Identity.Identity.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

public interface ITokenService
{
    string CreateAccessToken(User user, IList<Claim>? extraClaims = null);
    (string token, DateTime expires) GenerateRefreshToken();
    string HashToken(string token);
}

public class TokenService : ITokenService
{
    private readonly JwtOptions _opts;
    public TokenService(IOptions<JwtOptions> opts) => _opts = opts.Value;

    public string CreateAccessToken(User user, IList<Claim>? extraClaims = null)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? "")
        };
        if (extraClaims != null) claims.AddRange(extraClaims);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_opts.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string token, DateTime expires) GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToBase64String(randomBytes);
        var expires = DateTime.UtcNow.AddDays(_opts.RefreshTokenDays);
        return (token, expires);
    }

    public string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
