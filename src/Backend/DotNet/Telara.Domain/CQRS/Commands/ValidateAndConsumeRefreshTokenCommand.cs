using Mediator;

namespace Telara.Domain.CQRS.Commands;

public record RefreshTokenValidationResult(bool Success, long? UserId, Guid? FamilyId, bool ReuseDetected);

public record ValidateAndConsumeRefreshTokenCommand(string TokenHash) : IRequest<RefreshTokenValidationResult>;
