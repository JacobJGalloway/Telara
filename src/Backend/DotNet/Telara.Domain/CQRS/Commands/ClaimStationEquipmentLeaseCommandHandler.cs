using Mediator;
using Microsoft.EntityFrameworkCore;
using Telara.Domain.Data;
using Telara.Domain.Entities;
using Telara.Domain.Exceptions;
using Telara.Domain.Telemetry;

namespace Telara.Domain.CQRS.Commands;

public class ClaimStationEquipmentLeaseCommandHandler(TelaraDbContext db)
    : IRequestHandler<ClaimStationEquipmentLeaseCommand, ClaimStationEquipmentLeaseResult>
{
    public async ValueTask<ClaimStationEquipmentLeaseResult> Handle(ClaimStationEquipmentLeaseCommand request, CancellationToken cancellationToken)
    {
        var existing = await Find(request, cancellationToken);

        if (existing is not null)
        {
            ClaimLease(existing, request);
            await db.SaveChangesAsync(cancellationToken);
            return ToResult(existing);
        }

        var created = new StationEquipment
        {
            FacilityId = request.FacilityId,
            StationId = request.StationId,
            EquipmentId = request.EquipmentId,
            EquipmentTypeId = request.EquipmentTypeId,
            Status = EquipmentStatus.Operational,
            ActiveInstanceId = request.InstanceId,
            LeaseExpiresAtUtc = DateTime.UtcNow + request.LeaseDuration,
        };
        db.StationEquipment.Add(created);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return ToResult(created);
        }
        catch (DbUpdateException)
        {
            // Narrow race window (see ARCHITECTURE.md Get-or-Create/Lease Semantics): another
            // instance's insert won between our lookup and this save. The compound PK's unique
            // constraint caught it - re-fetch and fall through to the normal reclaim/conflict path.
            db.Entry(created).State = EntityState.Detached;
            var winner = await Find(request, cancellationToken)
                ?? throw new InvalidOperationException("StationEquipment insert conflicted but no row was found on re-fetch.");
            ClaimLease(winner, request);
            await db.SaveChangesAsync(cancellationToken);
            return ToResult(winner);
        }
    }

    private Task<StationEquipment?> Find(ClaimStationEquipmentLeaseCommand request, CancellationToken cancellationToken) =>
        db.StationEquipment.FirstOrDefaultAsync(
            e => e.FacilityId == request.FacilityId && e.StationId == request.StationId && e.EquipmentId == request.EquipmentId,
            cancellationToken);

    private static void ClaimLease(StationEquipment equipment, ClaimStationEquipmentLeaseCommand request)
    {
        var now = DateTime.UtcNow;
        var leaseIsStale = equipment.LeaseExpiresAtUtc is null || equipment.LeaseExpiresAtUtc < now;
        var leaseHeldBySelf = equipment.ActiveInstanceId == request.InstanceId;

        if (!leaseIsStale && !leaseHeldBySelf)
            throw new StationEquipmentAlreadyOperationalException(
                new TelemetryKey(request.FacilityId, request.StationId, TelemetryLevel.Equipment, request.EquipmentId));

        // Status is inherited as-is - lease state and status are independent concerns.
        equipment.ActiveInstanceId = request.InstanceId;
        equipment.LeaseExpiresAtUtc = now + request.LeaseDuration;
    }

    private static ClaimStationEquipmentLeaseResult ToResult(StationEquipment e) =>
        new(e.FacilityId, e.StationId, e.EquipmentId, e.Status, e.LeaseExpiresAtUtc!.Value);
}
