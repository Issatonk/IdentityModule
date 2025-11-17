using Microsoft.AspNetCore.Identity;

namespace Identity.Identity.Models;

public class User : IdentityUser<Guid>
{
    public User()
    {
        Id = Guid.CreateVersion7();
    }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
