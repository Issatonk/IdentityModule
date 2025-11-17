using Identity.Data;
using Identity.Identity.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Web;
public record RefreshCommand(string RefreshToken);
public record RefreshResponse(string AccessToken, string RefreshToken);

public class RefreshEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/auth/refresh", async (RefreshCommand cmd, RefreshHandler handler, HttpContext http) =>
        {
            var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await handler.Handle(cmd, ip);
            return Results.Ok(result);
        })
        .WithName("Refresh")
        .WithTags("Auth");
    }
}

public class RefreshHandler
{
    private readonly IdentityContext _db;
    private readonly ITokenService _tokenService;

    public RefreshHandler(IdentityContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<RefreshResponse> Handle(RefreshCommand cmd, string ipAddress)
    {
        var hashed = _tokenService.HashToken(cmd.RefreshToken);
        var rt = await _db.RefreshTokens.Include(x => x.User)
                    .FirstOrDefaultAsync(x => x.TokenHash == hashed);

        if (rt == null || rt.IsRevoked || rt.ExpiresAt <= DateTime.UtcNow)
            throw new ApplicationException("Invalid refresh token");

        rt.IsRevoked = true;
        _db.RefreshTokens.Update(rt);

        var user = rt.User;
        var accessToken = _tokenService.CreateAccessToken(user);
        var (newRefreshToken, newRefreshExpires) = _tokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = _tokenService.HashToken(newRefreshToken),
            ExpiresAt = newRefreshExpires,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            UserId = user.Id
        });

        await _db.SaveChangesAsync();
        return new RefreshResponse(accessToken, newRefreshToken);
    }
}
