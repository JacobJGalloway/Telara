using Mediator;

namespace Telara.Domain.CQRS.Commands;

// PredecessorStationIds is a list (rather than a single nullable id) even though only one entry
// is exercised by the registration form today - stations funneling multiple predecessors into a
// single successor is already valid under the existing Station.NextStationId/IsLoadingDock shape
// (each predecessor just sets its own NextStationId to the new station), so the API takes a list
// now rather than needing a breaking change once the form lets an operator pick more than one.
// Fan-out - one station feeding more than one successor - stays unsupported: NextStationId is a
// single self-referencing FK, one outgoing edge per station, by design (see ARCHITECTURE.md).
public record RegisterStationCommand(
    string FacilityId,
    string StationId,
    IReadOnlyList<string> PredecessorStationIds,
    bool IsLoadingDock) : IRequest<RegisterStationResult>;

public record RegisterStationResult(string FacilityId, string StationId, bool IsLoadingDock, IReadOnlyList<string> PredecessorStationIds);
