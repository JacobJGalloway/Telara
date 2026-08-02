using Mediator;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Queries;

public record GetUserByIdQuery(long Id) : IRequest<User?>;
