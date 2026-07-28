CREATE TABLE [dbo].[StationEquipmentReadings](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[station_equipment_id] [varchar](50) NOT NULL,
	[reading_datetime] [datetime2](0) NOT NULL,
	[belt_speed] [decimal](18, 4) NULL,
	[belt_temp] [decimal](8, 4) NULL,
	[oil_temp] [decimal](8, 4) NULL,
	[blade_speed] [decimal](18, 4) NULL,
	[blade_temp] [decimal](8, 4) NULL,
	[motor_speed] [decimal](18, 4) NULL,
	[motor_temp] [decimal](8, 4) NULL,
 CONSTRAINT [PK_StationEquipmentReadings] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]