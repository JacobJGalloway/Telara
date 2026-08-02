USE [telara_ops];
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [name] = 'Shift Supervisor')
BEGIN
    INSERT INTO [dbo].[Roles] ([name]) VALUES ('Shift Supervisor');
END
GO
