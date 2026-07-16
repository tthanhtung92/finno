using Finno.Identity.Domain.Identity;

using Microsoft.AspNetCore.Identity;

namespace Finno.Identity.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
