using Telara.Domain.Entities.Interfaces;

namespace Telara.Domain.Entities;

public class RefreshToken : IRefreshToken
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string TokenHash { get; set; } = default!;
    public Guid FamilyId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public User? User { get; set; }
}
