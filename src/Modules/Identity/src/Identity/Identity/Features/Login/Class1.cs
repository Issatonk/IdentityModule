using Identity.Data;
using Identity.Identity.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
public record LoginCommand(string UserName, string Password);
public record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn);
public static class LoginEndpoint
{
    public static WebApplication MapLoginEndpoint(this WebApplication app)
    {
        app.MapPost("/auth/login", async (LoginCommand cmd, LoginHandler handler, HttpContext http) =>
        {
            var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await handler.Handle(cmd, ip);
            return Results.Ok(result);
        })
        .WithName("Login")
        .WithTags("Auth");

        return app;
    }
}

public class LoginHandler
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IdentityContext _db;

    public LoginHandler(UserManager<User> userManager, ITokenService tokenService, IdentityContext db)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _db = db;
    }

    public async Task<LoginResponse> Handle(LoginCommand cmd, string ipAddress)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == cmd.UserName);
        if (user == null || !await _userManager.CheckPasswordAsync(user, cmd.Password))
            throw new ApplicationException("Invalid credentials");

        var accessToken = _tokenService.CreateAccessToken(user);
        var (refreshToken, refreshExpires) = _tokenService.GenerateRefreshToken();
        var hashed = _tokenService.HashToken(refreshToken);

        // Сохраняем refresh token
        _db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = hashed,
            ExpiresAt = refreshExpires,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            UserId = user.Id,
        });
        await _db.SaveChangesAsync();

        return new LoginResponse(accessToken, refreshToken, 60 * 15); // expires in seconds
    }
}
