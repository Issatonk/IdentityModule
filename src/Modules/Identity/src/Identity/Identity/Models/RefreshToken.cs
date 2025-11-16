namespace Identity.Identity.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByIp { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

}