using Microsoft.EntityFrameworkCore;
using Telara.Domain.Entities;

namespace Telara.Domain.Data;

public class TelaraDbContext(DbContextOptions<TelaraDbContext> options) : DbContext(options)
{
    public DbSet<StationEquipmentReading> StationEquipmentReadings => Set<StationEquipmentReading>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StationEquipmentReading>(entity =>
        {
            entity.ToTable("StationEquipmentReadings", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StationEquipmentId).HasColumnName("station_equipment_id");
            entity.Property(e => e.ReadingDateTime).HasColumnName("reading_datetime");
            entity.Property(e => e.BeltSpeed).HasColumnName("belt_speed").HasPrecision(18, 4);
            entity.Property(e => e.BeltTemp).HasColumnName("belt_temp").HasPrecision(8, 4);
            entity.Property(e => e.OilTemp).HasColumnName("oil_temp").HasPrecision(8, 4);
            entity.Property(e => e.BladeSpeed).HasColumnName("blade_speed").HasPrecision(18, 4);
            entity.Property(e => e.BladeTemp).HasColumnName("blade_temp").HasPrecision(8, 4);
            entity.Property(e => e.MotorSpeed).HasColumnName("motor_speed").HasPrecision(18, 4);
            entity.Property(e => e.MotorTemp).HasColumnName("motor_temp").HasPrecision(8, 4);
            entity.Property(e => e.BeltVibration).HasColumnName("belt_vibration").HasPrecision(8, 4);
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
    }
}
