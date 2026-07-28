using Mediator;

namespace Telara.Domain.CQRS.Commands;

public record RevokeRefreshTokenFamilyCommand(string TokenHash) : IRequest<bool>;
