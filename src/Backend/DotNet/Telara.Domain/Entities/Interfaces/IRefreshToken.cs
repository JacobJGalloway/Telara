namespace Telara.Domain.Entities.Interfaces;

public interface IRefreshToken
{
    long Id { get; set; }
    long UserId { get; set; }
    string TokenHash { get; set; }
    Guid FamilyId { get; set; }
    DateTime CreatedAtUtc { get; set; }
    DateTime ExpiresAtUtc { get; set; }
    DateTime? RevokedAtUtc { get; set; }
}
