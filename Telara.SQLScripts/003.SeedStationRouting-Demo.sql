USE [telara_ops];
GO

-- Chains the demo line registered via Postman (see
-- Telara.OpsApi/docs/postman-station-line-seed.md) - RegisterStation/RegisterStationEquipment
-- own row creation, but NextStationId/IsLoadingDock have no mutation yet, so this stitches the
-- routing directly. Run only after those four stations exist.
UPDATE [dbo].[Stations] SET [next_station_id] = 'ST-ASSEMBLY', [is_loading_dock] = 0
    WHERE [facility_id] = 'FAC-001' AND [station_id] = 'ST-INTAKE';
GO

UPDATE [dbo].[Stations] SET [next_station_id] = 'ST-PACK', [is_loading_dock] = 0
    WHERE [facility_id] = 'FAC-001' AND [station_id] = 'ST-ASSEMBLY';
GO

UPDATE [dbo].[Stations] SET [next_station_id] = 'ST-DOCK', [is_loading_dock] = 0
    WHERE [facility_id] = 'FAC-001' AND [station_id] = 'ST-PACK';
GO

UPDATE [dbo].[Stations] SET [next_station_id] = NULL, [is_loading_dock] = 1
    WHERE [facility_id] = 'FAC-001' AND [station_id] = 'ST-DOCK';
GO
