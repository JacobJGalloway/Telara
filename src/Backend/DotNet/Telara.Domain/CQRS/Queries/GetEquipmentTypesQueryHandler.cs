using Mediator;
using Microsoft.EntityFrameworkCore;
using Telara.Domain.Data;
using Telara.Domain.Entities;

namespace Telara.Domain.CQRS.Queries;

public class GetEquipmentTypesQueryHandler(TelaraDbContext db) : IRequestHandler<GetEquipmentTypesQuery, IQueryable<EquipmentType>>
{
    public ValueTask<IQueryable<EquipmentType>> Handle(GetEquipmentTypesQuery request, CancellationToken cancellationToken) =>
        ValueTask.FromResult(db.EquipmentTypes.AsNoTracking());
}
