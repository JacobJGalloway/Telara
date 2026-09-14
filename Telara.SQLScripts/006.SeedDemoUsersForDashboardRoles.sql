USE [telara_ops];
GO

-- Fake logins to exercise the three DESIGN.md dashboards end-to-end while the real multi-line
-- domain model (AssemblyLine/Line) doesn't exist yet - see DESIGN.md open items. Station Supervisor
-- is already covered by Jacob's demo user from 001 (now repointed at 'Station Supervisor' by 005).
--
-- Password for both: password123 (PBKDF2 via Microsoft.AspNetCore.Identity.PasswordHasher<T>,
-- not plaintext - same hash value as 001, hasher verifies fine reused across users).
--
-- assigned_station_id is NOT NULL but has no FK constraint and no meaning for these two roles
-- (Shift Manager/Plant Director are facility-scoped, not station-scoped); 'FAC-001' is a marker,
-- not a real station id, until line-scoped demo data replaces it next sprint.

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [email] = 'shift.manager@telara.demo')
BEGIN
    INSERT INTO [dbo].[Users]
        ([first_name], [last_name], [email], [password_hash], [assigned_station_id], [assigned_station_equipment_id], [assigned_role_id])
    VALUES
        ('Demo', 'ShiftManager', 'shift.manager@telara.demo',
         'AQAAAAIAAYagAAAAEH/uDYXrdYY0lvWHAIumY0Hig3lf4WmWZyTAdoCHtcMuWCzjxkpvUdZts4accVcXTg==',
         'FAC-001', NULL,
         (SELECT [id] FROM [dbo].[Roles] WHERE [name] = 'Shift Manager'));
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [email] = 'plant.director@telara.demo')
BEGIN
    INSERT INTO [dbo].[Users]
        ([first_name], [last_name], [email], [password_hash], [assigned_station_id], [assigned_station_equipment_id], [assigned_role_id])
    VALUES
        ('Demo', 'PlantDirector', 'plant.director@telara.demo',
         'AQAAAAIAAYagAAAAEH/uDYXrdYY0lvWHAIumY0Hig3lf4WmWZyTAdoCHtcMuWCzjxkpvUdZts4accVcXTg==',
         'FAC-001', NULL,
         (SELECT [id] FROM [dbo].[Roles] WHERE [name] = 'Plant Director'));
END
GO
