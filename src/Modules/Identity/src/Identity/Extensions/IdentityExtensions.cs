using Identity.Data;
using Identity.Dto;
using Identity.Identity.Features.GoogleOAuth;
using Identity.Identity.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Web;
using System.Text;

public static class IdentityExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<IdentityContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Identity
        services.AddIdentity<User, Role>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireDigit = true;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<IdentityContext>()
        .AddDefaultTokenProviders();

        // JWT Options
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        var jwtOpts = configuration.GetSection("Jwt").Get<JwtOptions>();

        // Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOpts.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOpts.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.Key)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        })
        .AddGoogle(options =>
        {
            options.ClientId = configuration["OAuth:Google:ClientId"];
            options.ClientSecret = configuration["OAuth:Google:ClientSecret"]; ;
        });

        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<RegisterHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshHandler>();
        services.AddScoped<GoogleOAuthHandler>();


        return services;
    }

    public static WebApplication UseIdentityModule(this WebApplication app)
    {
        var endpointsBuilder = app.MapGroup("/api");
        EndpointsProvider.RegisterAppEndpoints(endpointsBuilder);

        return app;
    }
}

public static class EndpointsProvider
{
    public static void RegisterAppEndpoints(RouteGroupBuilder endpointsBuilder)
    {
        endpointsBuilder.MapEndpoint<LoginEndpoint>();
        endpointsBuilder.MapEndpoint<RefreshEndpoint>();
        endpointsBuilder.MapEndpoint<RegisterNewUserEndpoint>();
        endpointsBuilder.MapEndpoint<GoogleAuthEndpoint>();
    }
}
