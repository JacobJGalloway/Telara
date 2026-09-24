USE [telara_ops];
GO

-- RegisterStationEquipment (see postman-station-line-seed.md) has no Status parameter, so every
-- piece of equipment on the FAC-001 demo line defaults to Idle (confirmed against a live registration
-- run, not Operational as first assumed here). Same reasoning as 003/004: no mutation owns
-- StationEquipment.Status transitions yet, so this stays raw SQL. Gives the workflow diagram
-- screen real status variety (critical/watch/operational) to render instead of four identical
-- amber prisms.
UPDATE [dbo].[StationEquipment] SET [status] = 'Operational'
    WHERE [facility_id] = 'FAC-001' AND [station_id] = 'ST-INTAKE' AND [equipment_id] = 'EQ-INTAKE-01';
GO

UPDATE [dbo].[StationEquipment] SET [status] = 'Faulted'
    WHERE [facility_id] = 'FAC-001' AND [station_id] = 'ST-ASSEMBLY' AND [equipment_id] = 'EQ-ASSEMBLY-01';
GO

UPDATE [dbo].[StationEquipment] SET [status] = 'Operational'
    WHERE [facility_id] = 'FAC-001' AND [station_id] = 'ST-PACK' AND [equipment_id] = 'EQ-PACK-01';
GO

-- ST-DOCK's EQ-DOCK-01 stays at its registration default (Idle) - not explicitly touched here.
