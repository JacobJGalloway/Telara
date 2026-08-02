using Microsoft.EntityFrameworkCore;
using Telara.Domain.Entities;

namespace Telara.Domain.Data;

public class TelaraDbContext(DbContextOptions<TelaraDbContext> options) : DbContext(options)
{
    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<StationEquipment> StationEquipment => Set<StationEquipment>();
    public DbSet<EquipmentType> EquipmentTypes => Set<EquipmentType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SensorReading>(entity =>
        {
            entity.ToTable("SensorReadings", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FacilityId).HasColumnName("facility_id");
            entity.Property(e => e.StationId).HasColumnName("station_id");
            entity.Property(e => e.EquipmentId).HasColumnName("equipment_id");
            entity.Property(e => e.SensorId).HasColumnName("sensor_id");
            entity.Property(e => e.ReadingType).HasColumnName("reading_type");
            entity.Property(e => e.Value).HasColumnName("value").HasPrecision(18, 4);
            entity.Property(e => e.ReadingAtUtc).HasColumnName("reading_at_utc");
            entity.HasIndex(e => new { e.FacilityId, e.StationId, e.EquipmentId, e.SensorId, e.ReadingAtUtc });
            entity.HasOne(e => e.StationEquipment)
                .WithMany()
                .HasForeignKey(e => new { e.FacilityId, e.StationId, e.EquipmentId });
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.AssignedStationId).HasColumnName("assigned_station_id");
            entity.Property(e => e.AssignedStationEquipmentId).HasColumnName("assigned_station_equipment_id");
            entity.Property(e => e.AssignedRoleId).HasColumnName("assigned_role_id");
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.AssignedRoleId);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(64);
            entity.Property(e => e.FamilyId).HasColumnName("family_id");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
            entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.FamilyId);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<Station>(entity =>
        {
            entity.ToTable("Stations", "dbo");
            entity.HasKey(e => new { e.FacilityId, e.StationId });
            entity.Property(e => e.FacilityId).HasColumnName("facility_id");
            entity.Property(e => e.StationId).HasColumnName("station_id");
            entity.Property(e => e.LastOperatorActionUtc).HasColumnName("last_operator_action_utc");
        });

        modelBuilder.Entity<EquipmentType>(entity =>
        {
            entity.ToTable("EquipmentTypes", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<StationEquipment>(entity =>
        {
            entity.ToTable("StationEquipment", "dbo");
            entity.HasKey(e => new { e.FacilityId, e.StationId, e.EquipmentId });
            entity.Property(e => e.FacilityId).HasColumnName("facility_id");
            entity.Property(e => e.StationId).HasColumnName("station_id");
            entity.Property(e => e.EquipmentId).HasColumnName("equipment_id");
            entity.Property(e => e.EquipmentTypeId).HasColumnName("equipment_type_id");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.LastSensorReadingUtc).HasColumnName("last_sensor_reading_utc");
            entity.Property(e => e.ActiveInstanceId).HasColumnName("active_instance_id");
            entity.Property(e => e.LeaseExpiresAtUtc).HasColumnName("lease_expires_at_utc");
            entity.HasOne(e => e.Station)
                .WithMany(s => s.Equipment)
                .HasForeignKey(e => new { e.FacilityId, e.StationId });
            entity.HasOne(e => e.EquipmentType)
                .WithMany()
                .HasForeignKey(e => e.EquipmentTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
