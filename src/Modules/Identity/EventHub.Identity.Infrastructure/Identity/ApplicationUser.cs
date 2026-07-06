using EventHub.Identity.Domain.Identity;

using Microsoft.AspNetCore.Identity;

namespace EventHub.Identity.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
