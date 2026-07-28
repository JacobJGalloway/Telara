USE [telara_ops];
GO

CREATE TABLE [dbo].[RefreshTokens](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[user_id] [bigint] NOT NULL,
	[token_hash] [varchar](64) NOT NULL,
	[family_id] [uniqueidentifier] NOT NULL,
	[created_at_utc] [datetime2](0) NOT NULL,
	[expires_at_utc] [datetime2](0) NOT NULL,
	[revoked_at_utc] [datetime2](0) NULL,
 CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [dbo].[RefreshTokens] ([token_hash] ASC)
GO

CREATE INDEX [IX_RefreshTokens_FamilyId] ON [dbo].[RefreshTokens] ([family_id] ASC)
GO

ALTER TABLE [dbo].[RefreshTokens]  WITH CHECK ADD  CONSTRAINT [FK_RefreshTokens_Users] FOREIGN KEY([user_id])
REFERENCES [dbo].[Users] ([id])
GO

ALTER TABLE [dbo].[RefreshTokens] CHECK CONSTRAINT [FK_RefreshTokens_Users]
GO
