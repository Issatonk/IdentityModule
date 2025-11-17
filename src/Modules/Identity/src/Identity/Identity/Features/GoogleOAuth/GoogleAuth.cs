using Identity.Data;
using Identity.Identity.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Web;
using System.Security.Claims;

namespace Identity.Identity.Features.GoogleOAuth;

public class GoogleAuthEndpoint : IEndpoint, IProviderName
{
    public string ProviderName => "Google";

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet($"/auth/external/{ProviderName.ToLower()}", async (HttpContext http) =>
        {
            var redirectUrl = $"api/auth/external/{ProviderName}/callback";
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Results.Challenge(properties, new List<string>() { ProviderName });
        });

        builder.MapGet($"/auth/external/{ProviderName}/callback", async (HttpContext http, [FromServices] GoogleOAuthHandler handler) =>
        {
            var result = await http.AuthenticateAsync(ProviderName);
            if (!result.Succeeded) return Results.Unauthorized();

            var externalId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;
            var userIp = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var response = await handler.Handle(new LoginGoogleOAuthCommand(email, externalId), userIp);

            return Results.Ok(new { accessToken = response.AccessToken, refreshToken = response.RefreshToken });
        });
    }
}
public record LoginGoogleOAuthCommand(string Email, string Name);

public record LoginGoogleOAuthResponse(string AccessToken, string RefreshToken, int ExpiresIn);
public class GoogleOAuthHandler
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IdentityContext _db;

    public GoogleOAuthHandler(UserManager<User> userManager, ITokenService tokenService, IdentityContext db)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _db = db;
    }

    public async Task<LoginGoogleOAuthResponse> Handle(LoginGoogleOAuthCommand cmd, string ipAddress)
    {
        var user = await _userManager.Users.SingleOrDefaultAsync(u => u.Email == cmd.Email);
        using var transaction = await _db.Database.BeginTransactionAsync();

        if (user == null)
        {
            user = new User { UserName = cmd.Name ?? cmd.Email, Email = cmd.Email };
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

        }

        var accessToken = _tokenService.CreateAccessToken(user);
        var (refreshToken, refreshExpires) = _tokenService.GenerateRefreshToken();
        var hashed = _tokenService.HashToken(refreshToken);

        _db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = hashed,
            ExpiresAt = refreshExpires,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            UserId = user.Id,
        });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        var minutes15 = 15 * 60;
        return new LoginGoogleOAuthResponse(accessToken, refreshToken, minutes15);
    }
}