using Mediator;
using HotChocolate.Authorization;
using Telara.Domain.CQRS.Queries;
using Telara.Domain.Entities;

namespace Telara.OpsApi.GraphQL;

[QueryType]
public static partial class Query
{
    public static string GetApiStatus() => "online";

    [Authorize]
    [UseFiltering]
    [UseSorting]
    public static async Task<IQueryable<SensorReading>> GetSensorReadings([Service] ISender mediator) =>
        await mediator.Send(new GetSensorReadingsQuery());
}
