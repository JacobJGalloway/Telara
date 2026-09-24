USE [telara_ops];
GO

-- No mutation owns Station.TargetOutputPerShift yet (same reasoning as 003's routing stitch),
-- so this stays raw SQL. Sets the shift target for the always-on demo generator instance
-- (Telara.OpsApi/appsettings.Development.json - demo-facility/station-01), which the dashboard's
-- shift output chart compares against StationOutputRecords.
UPDATE [dbo].[Stations] SET [target_output_per_shift] = 480
    WHERE [facility_id] = 'demo-facility' AND [station_id] = 'station-01';
GO
