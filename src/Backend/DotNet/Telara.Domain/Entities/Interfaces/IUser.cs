namespace Telara.Domain.Entities.Interfaces;

public interface IUser
{
    long Id { get; set; }
    string FirstName { get; set; }
    string LastName { get; set; }
    string Email { get; set; }
    string PasswordHash { get; set; }
    string AssignedStationId { get; set; }
    string? AssignedStationEquipmentId { get; set; }
    int? AssignedRoleId { get; set; }
}
