USE [telara_ops];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.StationEquipmentReadings') AND name = 'belt_vibration'
)
BEGIN
    ALTER TABLE [dbo].[StationEquipmentReadings]
    ADD [belt_vibration] [decimal](8, 4) NULL;
END
GO
