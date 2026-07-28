CREATE TABLE [dbo].[Users](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[first_name] [nvarchar](50) NOT NULL,
	[last_name] [nvarchar](50) NOT NULL,
	[email] [nvarchar](100) NOT NULL,
	[password_hash] [nvarchar](100) NOT NULL,
	[assigned_station_id] [nvarchar](50) NOT NULL,
	[assigned_station_equipment_id] [nvarchar](50) NULL,
	[assigned_role_id] [int] NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Users]  WITH CHECK ADD  CONSTRAINT [FK_Users_Roles] FOREIGN KEY([assigned_role_id])
REFERENCES [dbo].[Roles] ([id])
GO

ALTER TABLE [dbo].[Users] CHECK CONSTRAINT [FK_Users_Roles]
GO

CREATE UNIQUE INDEX [IX_Users_Email] ON [dbo].[Users] ([email] ASC)
GO