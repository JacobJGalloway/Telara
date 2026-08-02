using Telara.Domain.Entities.Interfaces;

namespace Telara.Domain.Entities;

public class User : IUser
{
    public long Id { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string AssignedStationId { get; set; } = default!;
    public string? AssignedStationEquipmentId { get; set; }
    public int? AssignedRoleId { get; set; }
    public Role? Role { get; set; }
}
