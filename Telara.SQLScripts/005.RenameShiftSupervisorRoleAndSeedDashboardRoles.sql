USE [telara_ops];
GO

-- DESIGN.md's three dashboard roles are Station Supervisor / Shift Manager / Plant Director.
-- 000 originally seeded 'Shift Supervisor' before that naming settled - rename it in place so
-- Jacob's existing demo user carries over rather than becoming orphaned.
IF EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [name] = 'Shift Supervisor')
BEGIN
    UPDATE [dbo].[Roles] SET [name] = 'Station Supervisor' WHERE [name] = 'Shift Supervisor';
END
ELSE IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [name] = 'Station Supervisor')
BEGIN
    INSERT INTO [dbo].[Roles] ([name]) VALUES ('Station Supervisor');
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [name] = 'Shift Manager')
BEGIN
    INSERT INTO [dbo].[Roles] ([name]) VALUES ('Shift Manager');
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [name] = 'Plant Director')
BEGIN
    INSERT INTO [dbo].[Roles] ([name]) VALUES ('Plant Director');
END
GO
