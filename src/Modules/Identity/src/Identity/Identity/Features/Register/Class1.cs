using Identity.Identity.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
public record RegisterCommand(string UserName, string Email, string Password);
public record RegisterResponse(string UserId, string UserName, string Email);

public static class RegisterEndpoint
{
    public static WebApplication MapRegisterEndpoint(this WebApplication app)
    {
        app.MapPost("/auth/register", async (RegisterCommand cmd, RegisterHandler handler) =>
        {
            var result = await handler.Handle(cmd);
            return Results.Ok(result);
        })
        .WithName("Register")
        .WithTags("Auth");

        return app;
    }
}


public class RegisterHandler
{
    private readonly UserManager<User> _userManager;

    public RegisterHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand cmd)
    {
        var user = new User { UserName = cmd.UserName, Email = cmd.Email };
        var result = await _userManager.CreateAsync(user, cmd.Password);

        if (!result.Succeeded)
            throw new ApplicationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        return new RegisterResponse(user.Id.ToString(), user.UserName!, user.Email!);
    }
}
