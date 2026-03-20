# Whistleblowing Database Schema

```sql


USE [WhistleblowerDB]
GO

CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED
);

CREATE TABLE [dbo].[Admins](
	[AdminId] [uniqueidentifier] NOT NULL,
	[Email] [nvarchar](256) NOT NULL,
	[PasswordHash] [nvarchar](512) NOT NULL,
	[MFASecret] [nvarchar](256) NOT NULL,
	[MFAEnabled] [bit] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[LastLoginAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Admins] PRIMARY KEY CLUSTERED  
);

CREATE TABLE [dbo].[AspNetRoleClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY CLUSTERED  
);

CREATE TABLE [dbo].[AspNetRoles](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](256) NULL,
	[NormalizedName] [nvarchar](256) NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED 
);

CREATE TABLE [dbo].[AspNetUserClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY CLUSTERED 
);

CREATE TABLE [dbo].[AspNetUserLogins](
	[LoginProvider] [nvarchar](450) NOT NULL,
	[ProviderKey] [nvarchar](450) NOT NULL,
	[ProviderDisplayName] [nvarchar](max) NULL,
	[UserId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY CLUSTERED 
);

CREATE TABLE [dbo].[AspNetUserRoles](
	[UserId] [uniqueidentifier] NOT NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY CLUSTERED 
);

CREATE TABLE [dbo].[AspNetUsers](
	[Id] [uniqueidentifier] NOT NULL,
	[InvestigatorId] [uniqueidentifier] NULL,
	[UserName] [nvarchar](256) NULL,
	[NormalizedUserName] [nvarchar](256) NULL,
	[Email] [nvarchar](256) NULL,
	[NormalizedEmail] [nvarchar](256) NULL,
	[EmailConfirmed] [bit] NOT NULL,
	[PasswordHash] [nvarchar](max) NULL,
	[SecurityStamp] [nvarchar](max) NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
	[PhoneNumber] [nvarchar](max) NULL,
	[PhoneNumberConfirmed] [bit] NOT NULL,
	[TwoFactorEnabled] [bit] NOT NULL,
	[LockoutEnd] [datetimeoffset](7) NULL,
	[LockoutEnabled] [bit] NOT NULL,
	[AccessFailedCount] [int] NOT NULL,
 CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED 
);

CREATE TABLE [dbo].[AspNetUserTokens](
	[UserId] [uniqueidentifier] NOT NULL,
	[LoginProvider] [nvarchar](450) NOT NULL,
	[Name] [nvarchar](450) NOT NULL,
	[Value] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY CLUSTERED 
);

CREATE TABLE [dbo].[AuditLogs](
	[LogId] [bigint] IDENTITY(1,1) NOT NULL,
	[ActorType] [tinyint] NOT NULL,
	[ActorId] [nvarchar](256) NULL,
	[Action] [nvarchar](100) NOT NULL,
	[TargetEntity] [nvarchar](50) NULL,
	[TargetId] [nvarchar](256) NULL,
	[Detail] [nvarchar](1000) NULL,
	[IPAddress] [nvarchar](45) NULL,
	[UserAgent] [nvarchar](512) NULL,
	[Timestamp] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED  
);

CREATE TABLE [dbo].[DeadlineTracking](
	[TrackingId] [uniqueidentifier] NOT NULL,
	[ReportId] [uniqueidentifier] NOT NULL,
	[DeadlineType] [tinyint] NOT NULL,
	[DueAt] [datetime2](7) NOT NULL,
	[CompletedAt] [datetime2](7) NULL,
	[IsOverdue] [bit] NOT NULL,
	[NotifiedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_DeadlineTracking] PRIMARY KEY CLUSTERED
);

CREATE TABLE [dbo].[Investigators](
	[InvestigatorId] [uniqueidentifier] NOT NULL,
	[Email] [nvarchar](256) NOT NULL,
	[PasswordHash] [nvarchar](512) NOT NULL,
	[MFASecret] [nvarchar](256) NOT NULL,
	[MFAEnabled] [bit] NOT NULL,
	[PublicKey] [varbinary](max) NULL,
	[EncryptedPrivateKey] [varbinary](max) NULL,
	[IsActive] [bit] NOT NULL,
	[LastLoginAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NOT NULL,
	[PrivateKeyIv] [varbinary](max) NULL,
	[PrivateKeySalt] [varbinary](max) NULL,
 CONSTRAINT [PK_Investigators] PRIMARY KEY CLUSTERED 
);

CREATE TABLE [dbo].[Messages](
	[MessageId] [uniqueidentifier] NOT NULL,
	[ReportId] [uniqueidentifier] NOT NULL,
	[SenderRole] [tinyint] NOT NULL,
	[EncryptedContent] [varbinary](max) NOT NULL,
	[EncryptedKeyEnvelope] [varbinary](max) NOT NULL,
	[IsRead] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[WBKeyEnvelope] [varbinary](max) NULL,
 CONSTRAINT [PK_Messages] PRIMARY KEY CLUSTERED 
);
 

CREATE TABLE [dbo].[NotificationQueue](
	[NotificationId] [uniqueidentifier] NOT NULL,
	[RecipientId] [uniqueidentifier] NOT NULL,
	[NotificationType] [tinyint] NOT NULL,
	[ReferenceId] [uniqueidentifier] NOT NULL,
	[Subject] [nvarchar](256) NOT NULL,
	[IsSent] [bit] NOT NULL,
	[SentAt] [datetime2](7) NULL,
	[RetryCount] [int] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_NotificationQueue] PRIMARY KEY CLUSTERED 
);


CREATE TABLE [dbo].[PlatformSettings](
	[SettingId] [int] IDENTITY(1,1) NOT NULL,
	[SettingKey] [nvarchar](100) NOT NULL,
	[SettingValue] [nvarchar](max) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[UpdatedAt] [datetime2](7) NOT NULL,
	[UpdatedBy] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_PlatformSettings] PRIMARY KEY CLUSTERED
);


CREATE TABLE [dbo].[ReportAttachments](
	[AttachmentId] [uniqueidentifier] NOT NULL,
	[ReportId] [uniqueidentifier] NOT NULL,
	[StoragePath] [nvarchar](500) NOT NULL,
	[EncryptedKeyEnvelope] [varbinary](max) NOT NULL,
	[WBKeyEnvelope] [varbinary](max) NOT NULL,
	[EncryptedFileName] [varbinary](512) NOT NULL,
	[MimeType] [nvarchar](100) NOT NULL,
	[FileSize] [bigint] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[SanitizationStoragePath] [nvarchar](500) NULL,
	[SanitizationKey] [varbinary](max) NULL,
	[SanitizationStatus] [tinyint] NOT NULL,
	[SanitizationError] [nvarchar](500) NULL,
	[SanitizedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_ReportAttachments] PRIMARY KEY CLUSTERED
);


CREATE TABLE [dbo].[ReportCategories](
	[CategoryId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[DisplayOrder] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ReportCategories] PRIMARY KEY CLUSTERED 
 );

 
CREATE TABLE [dbo].[Reports](
	[ReportId] [uniqueidentifier] NOT NULL,
	[CaseNumber] [nvarchar](20) NOT NULL,
	[TokenHash] [varbinary](64) NOT NULL,
	[CategoryId] [int] NULL,
	[Status] [tinyint] NOT NULL,
	[EncryptedContent] [varbinary](max) NOT NULL,
	[EncryptedKeyEnvelope] [varbinary](max) NOT NULL,
	[WBKeyEnvelope] [varbinary](max) NOT NULL,
	[WBPublicKey] [varbinary](max) NOT NULL,
	[SelfIdentified] [bit] NOT NULL,
	[EncryptedIdentity] [varbinary](max) NULL,
	[EncryptedIdentityKeyEnvelope] [varbinary](max) NULL,
	[EncryptedWBPrivateKey] [varbinary](max) NOT NULL,
	[WBKeySalt] [varbinary](32) NOT NULL,
	[AcknowledgedAt] [datetime2](7) NULL,
	[AcknowledgementDueAt] [datetime2](7) NOT NULL,
	[FeedbackDueAt] [datetime2](7) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Reports] PRIMARY KEY CLUSTERED 
  );
GO
-- End of Schema Script
