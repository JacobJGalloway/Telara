USE [telara_ops];
GO

-- Password: password123 (PBKDF2 via Microsoft.AspNetCore.Identity.PasswordHasher<T>, not plaintext)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [email] = 'jacobjgalloway@gmail.com')
BEGIN
    INSERT INTO [dbo].[Users]
        ([first_name], [last_name], [email], [password_hash], [assigned_station_id], [assigned_station_equipment_id], [assigned_role_id])
    VALUES
        ('Jacob', 'Galloway', 'jacobjgalloway@gmail.com',
         'AQAAAAIAAYagAAAAEH/uDYXrdYY0lvWHAIumY0Hig3lf4WmWZyTAdoCHtcMuWCzjxkpvUdZts4accVcXTg==',
         'ST001', NULL,
         (SELECT [id] FROM [dbo].[Roles] WHERE [name] = 'Shift Supervisor'));
END
GO
