using Identity.Identity.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Identity.Data;

public class IdentityContext : IdentityDbContext<User, Role, Guid, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public IdentityContext(DbContextOptions<IdentityContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(b =>
        {
            b.ToTable("Users");
            b.HasMany(e => e.RefreshTokens).WithOne(rt => rt.User)
             .HasForeignKey(rt => rt.UserId).IsRequired();

            b.HasKey(u => u.Id);

            b.Property(u => u.Id)
                  .ValueGeneratedNever();
        });

        builder.Entity<Role>(b => b.ToTable("Roles"));
        builder.Entity<UserClaim>(b => b.ToTable("UserClaims"));
        builder.Entity<UserRole>(b => b.ToTable("UserRoles"));
        builder.Entity<UserLogin>(b => b.ToTable("UserLogins"));
        builder.Entity<RoleClaim>(b => b.ToTable("RoleClaims"));
        builder.Entity<UserToken>(b => b.ToTable("UserTokens"));

        // RefreshToken
        builder.Entity<RefreshToken>(b =>
        {
            b.ToTable("RefreshTokens");
            b.HasKey(rt => rt.Id);
            b.Property(rt => rt.TokenHash).IsRequired();
            b.HasIndex(rt => rt.TokenHash).IsUnique(false);
        });
    }

}
