USE [telara_ops];
GO

-- Reference table, same shape as 000.SeedRolesTable.sql - no service/command owns EquipmentTypes
-- rows, so this stays raw SQL rather than going through a mutation.
IF NOT EXISTS (SELECT 1 FROM [dbo].[EquipmentTypes] WHERE [name] = 'Conveyor')
BEGIN
    INSERT INTO [dbo].[EquipmentTypes] ([name]) VALUES ('Conveyor');
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[EquipmentTypes] WHERE [name] = 'Press')
BEGIN
    INSERT INTO [dbo].[EquipmentTypes] ([name]) VALUES ('Press');
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[EquipmentTypes] WHERE [name] = 'Scanner')
BEGIN
    INSERT INTO [dbo].[EquipmentTypes] ([name]) VALUES ('Scanner');
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[EquipmentTypes] WHERE [name] = 'Packer')
BEGIN
    INSERT INTO [dbo].[EquipmentTypes] ([name]) VALUES ('Packer');
END
GO
