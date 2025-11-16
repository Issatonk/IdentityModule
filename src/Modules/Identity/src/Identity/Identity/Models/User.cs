using Microsoft.AspNetCore.Identity;

namespace Identity.Identity.Models;

public class User : IdentityUser<Guid>
{
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
