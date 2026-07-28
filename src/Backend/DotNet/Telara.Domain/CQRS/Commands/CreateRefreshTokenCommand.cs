using Mediator;

namespace Telara.Domain.CQRS.Commands;

public record CreateRefreshTokenCommand(long UserId, string TokenHash, Guid FamilyId, DateTime ExpiresAtUtc) : IRequest<long>;
