using Mediator;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Queries;

public record AuthenticateUserQuery(string Email, string Password) : IRequest<User?>;
