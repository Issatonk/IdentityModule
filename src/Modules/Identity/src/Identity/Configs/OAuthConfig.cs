using Microsoft.Extensions.Configuration;

namespace Identity.Configs;

internal static class OAuthConfig
{
    public static bool GoogleEnabled { get; private set; }
    public static string GoogleClientId { get; private set; } = "";
    public static string GoogleClientSecret { get; private set; } = "";

    public static void Init(IConfiguration configuration)
    {
        GoogleEnabled = configuration.GetValue<bool>("OAuth:Google:Enabled");
        GoogleClientId = configuration["OAuth:Google:ClientId"] ?? "";
        GoogleClientSecret = configuration["OAuth:Google:ClientSecret"] ?? "";
    }
}
