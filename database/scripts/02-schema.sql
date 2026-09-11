IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Description] nvarchar(max) NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [Designation] nvarchar(max) NULL,
        [Department] nvarchar(max) NULL,
        [AvatarUrl] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [LastLoginAt] datetimeoffset NULL,
        [LastLoginIp] nvarchar(max) NULL,
        [MustChangePassword] bit NOT NULL,
        [RefreshToken] nvarchar(max) NULL,
        [RefreshTokenExpiresAt] datetimeoffset NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [Timestamp] datetimeoffset NOT NULL,
        [UserId] nvarchar(450) NULL,
        [UserName] nvarchar(256) NULL,
        [Action] nvarchar(60) NOT NULL,
        [EntityName] nvarchar(150) NOT NULL,
        [EntityId] nvarchar(100) NULL,
        [Changes] nvarchar(max) NULL,
        [IpAddress] nvarchar(64) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [AwarenessProgrammes] (
        [Id] int NOT NULL IDENTITY,
        [ProgrammeCode] nvarchar(60) NOT NULL,
        [Title] nvarchar(400) NOT NULL,
        [Description] nvarchar(max) NULL,
        [ProgrammeType] nvarchar(100) NOT NULL,
        [Agency] nvarchar(100) NULL,
        [State] nvarchar(120) NULL,
        [District] nvarchar(120) NULL,
        [Venue] nvarchar(500) NULL,
        [StartDate] datetimeoffset NOT NULL,
        [EndDate] datetimeoffset NULL,
        [RegisteredCount] int NOT NULL,
        [Capacity] int NULL,
        [RegistrationUrl] nvarchar(500) NULL,
        [ProgrammeStatus] int NOT NULL,
        [Status] int NOT NULL,
        [PublishedAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_AwarenessProgrammes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [Banners] (
        [Id] int NOT NULL IDENTITY,
        [Eyebrow] nvarchar(200) NULL,
        [Title] nvarchar(300) NOT NULL,
        [HighlightedTitle] nvarchar(300) NULL,
        [Subtitle] nvarchar(800) NULL,
        [ImageUrl] nvarchar(500) NOT NULL,
        [MobileImageUrl] nvarchar(500) NULL,
        [AltText] nvarchar(300) NULL,
        [PrimaryButtonText] nvarchar(120) NULL,
        [PrimaryButtonUrl] nvarchar(500) NULL,
        [SecondaryButtonText] nvarchar(120) NULL,
        [SecondaryButtonUrl] nvarchar(500) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [StartsAt] datetimeoffset NULL,
        [EndsAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Banners] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [ContactMessages] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [Phone] nvarchar(40) NULL,
        [Organisation] nvarchar(300) NULL,
        [UdyamNumber] nvarchar(60) NULL,
        [State] nvarchar(120) NULL,
        [Subject] nvarchar(400) NOT NULL,
        [Message] nvarchar(4000) NOT NULL,
        [Category] nvarchar(120) NULL,
        [Status] int NOT NULL,
        [AssignedTo] nvarchar(256) NULL,
        [InternalNotes] nvarchar(max) NULL,
        [RespondedAt] datetimeoffset NULL,
        [IpAddress] nvarchar(64) NULL,
        [UserAgent] nvarchar(500) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_ContactMessages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [Documents] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(400) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [Category] int NOT NULL,
        [FileUrl] nvarchar(500) NOT NULL,
        [FileType] nvarchar(20) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [Language] nvarchar(50) NULL,
        [Version] nvarchar(50) NULL,
        [DocumentDate] datetimeoffset NULL,
        [SortOrder] int NOT NULL,
        [IsFeatured] bit NOT NULL,
        [DownloadCount] int NOT NULL,
        [Status] int NOT NULL,
        [PublishedAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Documents] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [Faqs] (
        [Id] int NOT NULL IDENTITY,
        [Question] nvarchar(600) NOT NULL,
        [Answer] nvarchar(max) NOT NULL,
        [Category] nvarchar(120) NULL,
        [SortOrder] int NOT NULL,
        [IsFeatured] bit NOT NULL,
        [HelpfulCount] int NOT NULL,
        [Status] int NOT NULL,
        [PublishedAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Faqs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [GalleryAlbums] (
        [Id] int NOT NULL IDENTITY,
        [Slug] nvarchar(200) NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [CoverImageUrl] nvarchar(500) NULL,
        [Location] nvarchar(200) NULL,
        [EventDate] datetimeoffset NULL,
        [SortOrder] int NOT NULL,
        [Status] int NOT NULL,
        [PublishedAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_GalleryAlbums] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [LoginPortals] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(250) NOT NULL,
        [Audience] nvarchar(150) NULL,
        [Description] nvarchar(600) NULL,
        [Icon] nvarchar(80) NULL,
        [LoginUrl] nvarchar(500) NULL,
        [LoginText] nvarchar(80) NULL,
        [RegisterUrl] nvarchar(500) NULL,
        [RegisterText] nvarchar(80) NULL,
        [AccentColor] nvarchar(20) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [OpenInNewTab] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_LoginPortals] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [MediaAssets] (
        [Id] int NOT NULL IDENTITY,
        [FileName] nvarchar(300) NOT NULL,
        [StoredFileName] nvarchar(300) NOT NULL,
        [Url] nvarchar(500) NOT NULL,
        [ThumbnailUrl] nvarchar(500) NULL,
        [ContentType] nvarchar(150) NOT NULL,
        [SizeBytes] bigint NOT NULL,
        [Width] int NULL,
        [Height] int NULL,
        [AltText] nvarchar(300) NULL,
        [Caption] nvarchar(500) NULL,
        [Folder] nvarchar(120) NULL,
        [Checksum] nvarchar(80) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_MediaAssets] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [Pages] (
        [Id] int NOT NULL IDENTITY,
        [Slug] nvarchar(200) NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [ShortTitle] nvarchar(150) NULL,
        [Summary] nvarchar(1000) NULL,
        [Body] nvarchar(max) NULL,
        [Template] int NOT NULL,
        [CustomComponent] nvarchar(100) NULL,
        [ParentId] int NULL,
        [SortOrder] int NOT NULL,
        [BannerImageUrl] nvarchar(500) NULL,
        [BannerCaption] nvarchar(300) NULL,
        [ShowInMainMenu] bit NOT NULL,
        [ShowSidebarNav] bit NOT NULL,
        [MetaTitle] nvarchar(300) NULL,
        [MetaDescription] nvarchar(500) NULL,
        [MetaKeywords] nvarchar(500) NULL,
        [OgImageUrl] nvarchar(500) NULL,
        [Status] int NOT NULL,
        [PublishedAt] datetimeoffset NULL,
        [ViewCount] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Pages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Pages_Pages_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Pages] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [Partners] (
        [Id] int NOT NULL IDENTITY,
        [Type] int NOT NULL,
        [Name] nvarchar(400) NOT NULL,
        [ShortName] nvarchar(100) NULL,
        [Description] nvarchar(max) NULL,
        [LogoUrl] nvarchar(500) NULL,
        [WebsiteUrl] nvarchar(500) NULL,
        [ContactUrl] nvarchar(500) NULL,
        [Email] nvarchar(256) NULL,
        [Phone] nvarchar(60) NULL,
        [Address] nvarchar(600) NULL,
        [State] nvarchar(120) NULL,
        [City] nvarchar(120) NULL,
        [RegistrationNumber] nvarchar(100) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [IsFeatured] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Partners] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [Posts] (
        [Id] int NOT NULL IDENTITY,
        [Type] int NOT NULL,
        [Slug] nvarchar(250) NOT NULL,
        [Title] nvarchar(400) NOT NULL,
        [Excerpt] nvarchar(1000) NULL,
        [Body] nvarchar(max) NULL,
        [CoverImageUrl] nvarchar(500) NULL,
        [Author] nvarchar(200) NULL,
        [AttachmentUrl] nvarchar(500) NULL,
        [AttachmentLabel] nvarchar(200) NULL,
        [IsFeatured] bit NOT NULL,
        [ShowInTicker] bit NOT NULL,
        [ExpiresAt] datetimeoffset NULL,
        [Status] int NOT NULL,
        [PublishedAt] datetimeoffset NULL,
        [ViewCount] int NOT NULL,
        [MetaDescription] nvarchar(500) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Posts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [SchemeComponents] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(250) NOT NULL,
        [ShortDescription] nvarchar(500) NULL,
        [Description] nvarchar(max) NULL,
        [Icon] nvarchar(80) NULL,
        [LinkUrl] nvarchar(500) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_SchemeComponents] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [SchemeLevels] (
        [Id] int NOT NULL IDENTITY,
        [Level] int NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [BadgeLabel] nvarchar(60) NULL,
        [Tagline] nvarchar(300) NULL,
        [Description] nvarchar(max) NULL,
        [Deliverables] nvarchar(max) NULL,
        [FeeStructure] nvarchar(500) NULL,
        [Duration] nvarchar(100) NULL,
        [IconUrl] nvarchar(500) NULL,
        [CertificateImageUrl] nvarchar(500) NULL,
        [AccentColor] nvarchar(20) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_SchemeLevels] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [SiteSettings] (
        [Id] int NOT NULL IDENTITY,
        [Key] nvarchar(150) NOT NULL,
        [Value] nvarchar(max) NULL,
        [DisplayName] nvarchar(250) NULL,
        [Description] nvarchar(600) NULL,
        [Group] nvarchar(100) NOT NULL,
        [DataType] nvarchar(40) NOT NULL,
        [SortOrder] int NOT NULL,
        [IsPublic] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_SiteSettings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [Statistics] (
        [Id] int NOT NULL IDENTITY,
        [Label] nvarchar(200) NOT NULL,
        [Value] bigint NOT NULL,
        [Prefix] nvarchar(20) NULL,
        [Suffix] nvarchar(20) NULL,
        [Icon] nvarchar(80) NULL,
        [LinkUrl] nvarchar(500) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [SourceQueryKey] nvarchar(100) NULL,
        [LastSyncedAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Statistics] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [Subscribers] (
        [Id] int NOT NULL IDENTITY,
        [Email] nvarchar(256) NOT NULL,
        [Name] nvarchar(200) NULL,
        [IsConfirmed] bit NOT NULL,
        [ConfirmationToken] nvarchar(200) NULL,
        [ConfirmedAt] datetimeoffset NULL,
        [UnsubscribedAt] datetimeoffset NULL,
        [Source] nvarchar(120) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Subscribers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [Testimonials] (
        [Id] int NOT NULL IDENTITY,
        [UnitName] nvarchar(300) NOT NULL,
        [PersonName] nvarchar(200) NULL,
        [Designation] nvarchar(200) NULL,
        [Location] nvarchar(200) NULL,
        [Sector] nvarchar(200) NULL,
        [Quote] nvarchar(2000) NOT NULL,
        [PhotoUrl] nvarchar(500) NULL,
        [VideoUrl] nvarchar(500) NULL,
        [AchievedLevel] int NULL,
        [ImpactHighlight] nvarchar(300) NULL,
        [SortOrder] int NOT NULL,
        [Status] int NOT NULL,
        [PublishedAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Testimonials] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [GalleryImages] (
        [Id] int NOT NULL IDENTITY,
        [AlbumId] int NOT NULL,
        [ImageUrl] nvarchar(500) NOT NULL,
        [ThumbnailUrl] nvarchar(500) NULL,
        [Caption] nvarchar(400) NULL,
        [AltText] nvarchar(300) NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_GalleryImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GalleryImages_GalleryAlbums_AlbumId] FOREIGN KEY ([AlbumId]) REFERENCES [GalleryAlbums] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [MenuItems] (
        [Id] int NOT NULL IDENTITY,
        [Location] int NOT NULL,
        [Label] nvarchar(150) NOT NULL,
        [Url] nvarchar(500) NULL,
        [PageId] int NULL,
        [ParentId] int NULL,
        [SortOrder] int NOT NULL,
        [OpenInNewTab] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [Icon] nvarchar(80) NULL,
        [IsHighlighted] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_MenuItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MenuItems_MenuItems_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [MenuItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MenuItems_Pages_PageId] FOREIGN KEY ([PageId]) REFERENCES [Pages] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE TABLE [PageBlocks] (
        [Id] int NOT NULL IDENTITY,
        [PageId] int NOT NULL,
        [Type] int NOT NULL,
        [SortOrder] int NOT NULL,
        [IsVisible] bit NOT NULL,
        [Eyebrow] nvarchar(200) NULL,
        [Heading] nvarchar(300) NULL,
        [SubHeading] nvarchar(500) NULL,
        [Body] nvarchar(max) NULL,
        [ImageUrl] nvarchar(500) NULL,
        [VideoUrl] nvarchar(500) NULL,
        [PrimaryLinkText] nvarchar(120) NULL,
        [PrimaryLinkUrl] nvarchar(500) NULL,
        [SecondaryLinkText] nvarchar(120) NULL,
        [SecondaryLinkUrl] nvarchar(500) NULL,
        [SettingsJson] nvarchar(max) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_PageBlocks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PageBlocks_Pages_PageId] FOREIGN KEY ([PageId]) REFERENCES [Pages] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_EntityName_EntityId] ON [AuditLogs] ([EntityName], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_Timestamp] ON [AuditLogs] ([Timestamp]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AwarenessProgrammes_ProgrammeCode] ON [AwarenessProgrammes] ([ProgrammeCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AwarenessProgrammes_ProgrammeType_ProgrammeStatus] ON [AwarenessProgrammes] ([ProgrammeType], [ProgrammeStatus]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AwarenessProgrammes_State_StartDate] ON [AwarenessProgrammes] ([State], [StartDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Banners_IsActive_SortOrder] ON [Banners] ([IsActive], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ContactMessages_Status_CreatedAt] ON [ContactMessages] ([Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Documents_Category_SortOrder] ON [Documents] ([Category], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Faqs_Category_SortOrder] ON [Faqs] ([Category], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_GalleryAlbums_Slug] ON [GalleryAlbums] ([Slug]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_GalleryImages_AlbumId_SortOrder] ON [GalleryImages] ([AlbumId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LoginPortals_IsActive_SortOrder] ON [LoginPortals] ([IsActive], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MediaAssets_Folder_CreatedAt] ON [MediaAssets] ([Folder], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MenuItems_Location_SortOrder] ON [MenuItems] ([Location], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MenuItems_PageId] ON [MenuItems] ([PageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MenuItems_ParentId] ON [MenuItems] ([ParentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PageBlocks_PageId_SortOrder] ON [PageBlocks] ([PageId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Pages_ParentId] ON [Pages] ([ParentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Pages_Slug] ON [Pages] ([Slug]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Pages_Status_SortOrder] ON [Pages] ([Status], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Partners_Type_SortOrder] ON [Partners] ([Type], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Posts_Slug] ON [Posts] ([Slug]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Posts_Type_Status_PublishedAt] ON [Posts] ([Type], [Status], [PublishedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SchemeComponents_SortOrder] ON [SchemeComponents] ([SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SchemeLevels_SortOrder] ON [SchemeLevels] ([SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SiteSettings_Key] ON [SiteSettings] ([Key]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Statistics_IsActive_SortOrder] ON [Statistics] ([IsActive], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Subscribers_Email] ON [Subscribers] ([Email]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Testimonials_Status_SortOrder] ON [Testimonials] ([Status], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904091156_InitialCreate'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260904091156_InitialCreate', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905102605_AddSiteSettingSection'
)
BEGIN
    ALTER TABLE [SiteSettings] ADD [Section] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905102605_AddSiteSettingSection'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260905102605_AddSiteSettingSection', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905104712_AddGalleryVideoUrl'
)
BEGIN
    ALTER TABLE [GalleryImages] ADD [VideoUrl] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905104712_AddGalleryVideoUrl'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260905104712_AddGalleryVideoUrl', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905113435_AddGalleryImageIsActive'
)
BEGIN
    ALTER TABLE [GalleryImages] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260905113435_AddGalleryImageIsActive'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260905113435_AddGalleryImageIsActive', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906013704_AddEnquiryAgency'
)
BEGIN
    ALTER TABLE [ContactMessages] ADD [Agency] nvarchar(120) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906013704_AddEnquiryAgency'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906013704_AddEnquiryAgency', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906014337_AddEnquiryAttachments'
)
BEGIN
    ALTER TABLE [ContactMessages] ADD [AttachmentsJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906014337_AddEnquiryAttachments'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906014337_AddEnquiryAttachments', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906083723_AddBenefits'
)
BEGIN
    CREATE TABLE [Benefits] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [Subtitle] nvarchar(300) NULL,
        [ImageUrl] nvarchar(500) NULL,
        [Icon] nvarchar(80) NULL,
        [LinkUrl] nvarchar(500) NULL,
        [LinkText] nvarchar(60) NULL,
        [OpenInNewTab] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Benefits] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906083723_AddBenefits'
)
BEGIN
    CREATE INDEX [IX_Benefits_IsActive_SortOrder] ON [Benefits] ([IsActive], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906083723_AddBenefits'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906083723_AddBenefits', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906085158_AddIncentives'
)
BEGIN
    CREATE TABLE [Incentives] (
        [Id] int NOT NULL IDENTITY,
        [Category] int NOT NULL,
        [Level] nvarchar(80) NULL,
        [State] nvarchar(120) NULL,
        [Title] nvarchar(400) NOT NULL,
        [Description] nvarchar(max) NULL,
        [IssuerName] nvarchar(300) NULL,
        [IssuerLogoUrl] nvarchar(500) NULL,
        [ContactName] nvarchar(300) NULL,
        [ContactEmail] nvarchar(256) NULL,
        [ContactPhone] nvarchar(120) NULL,
        [Document1Url] nvarchar(500) NULL,
        [Document1Label] nvarchar(120) NULL,
        [Document2Url] nvarchar(500) NULL,
        [Document2Label] nvarchar(120) NULL,
        [AvailUrl] nvarchar(500) NULL,
        [AvailLabel] nvarchar(60) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Incentives] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906085158_AddIncentives'
)
BEGIN
    CREATE INDEX [IX_Incentives_Category_IsActive_SortOrder] ON [Incentives] ([Category], [IsActive], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906085158_AddIncentives'
)
BEGIN
    CREATE INDEX [IX_Incentives_State] ON [Incentives] ([State]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906085158_AddIncentives'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906085158_AddIncentives', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906091130_AddIncentiveVideo'
)
BEGIN
    ALTER TABLE [Incentives] ADD [VideoUrl] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906091130_AddIncentiveVideo'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906091130_AddIncentiveVideo', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906093725_AddAgencyEnquiryRouting'
)
BEGIN
    ALTER TABLE [Partners] ADD [EnquiryEmail] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906093725_AddAgencyEnquiryRouting'
)
BEGIN
    ALTER TABLE [Partners] ADD [EnquiryFormUrl] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906093725_AddAgencyEnquiryRouting'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906093725_AddAgencyEnquiryRouting', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911102204_AddHelpdeskAndIntegrations'
)
BEGIN
    ALTER TABLE [ContactMessages] ADD [HelpdeskAttempts] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911102204_AddHelpdeskAndIntegrations'
)
BEGIN
    ALTER TABLE [ContactMessages] ADD [HelpdeskLastError] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911102204_AddHelpdeskAndIntegrations'
)
BEGIN
    ALTER TABLE [ContactMessages] ADD [HelpdeskNextAttemptAt] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911102204_AddHelpdeskAndIntegrations'
)
BEGIN
    ALTER TABLE [ContactMessages] ADD [HelpdeskSentAt] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911102204_AddHelpdeskAndIntegrations'
)
BEGIN
    ALTER TABLE [ContactMessages] ADD [HelpdeskStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911102204_AddHelpdeskAndIntegrations'
)
BEGIN
    ALTER TABLE [ContactMessages] ADD [HelpdeskTicketId] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911102204_AddHelpdeskAndIntegrations'
)
BEGIN
    ALTER TABLE [ContactMessages] ADD [HelpdeskTicketNumber] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911102204_AddHelpdeskAndIntegrations'
)
BEGIN
    ALTER TABLE [ContactMessages] ADD [IssueCategory] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911102204_AddHelpdeskAndIntegrations'
)
BEGIN
    ALTER TABLE [ContactMessages] ADD [IssueSubCategory] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911102204_AddHelpdeskAndIntegrations'
)
BEGIN
    ALTER TABLE [ContactMessages] ADD [IssueType] nvarchar(120) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911102204_AddHelpdeskAndIntegrations'
)
BEGIN
    ALTER TABLE [ContactMessages] ADD [UserType] nvarchar(120) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911102204_AddHelpdeskAndIntegrations'
)
BEGIN
    CREATE TABLE [DocumentTexts] (
        [DocumentId] int NOT NULL,
        [Text] nvarchar(max) NOT NULL,
        [SourceUrl] nvarchar(500) NOT NULL,
        [ExtractedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_DocumentTexts] PRIMARY KEY ([DocumentId]),
        CONSTRAINT [FK_DocumentTexts_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911102204_AddHelpdeskAndIntegrations'
)
BEGIN
    CREATE TABLE [ExternalIntegrations] (
        [Id] int NOT NULL IDENTITY,
        [Key] nvarchar(60) NOT NULL,
        [Mode] nvarchar(20) NOT NULL,
        [Title] nvarchar(200) NULL,
        [Intro] nvarchar(2000) NULL,
        [Url] nvarchar(1000) NULL,
        [EmbedCode] nvarchar(max) NULL,
        [FrameHeight] int NOT NULL,
        [ApiUrl] nvarchar(1000) NULL,
        [ApiMethod] nvarchar(10) NOT NULL,
        [ApiHeaderName] nvarchar(100) NULL,
        [ApiHeaderValue] nvarchar(2000) NULL,
        [ApiBodyTemplate] nvarchar(max) NULL,
        [ApiResultPath] nvarchar(200) NULL,
        [ApiFieldMap] nvarchar(max) NULL,
        [ApiTotalPath] nvarchar(200) NULL,
        [InputLabel] nvarchar(120) NULL,
        [LastCheckedAt] datetimeoffset NULL,
        [LastCheckSucceeded] bit NULL,
        [LastCheckResult] nvarchar(1000) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_ExternalIntegrations] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911102204_AddHelpdeskAndIntegrations'
)
BEGIN
    CREATE TABLE [HelpdeskConnections] (
        [Id] int NOT NULL IDENTITY,
        [IsEnabled] bit NOT NULL,
        [PartnerId] int NULL,
        [AccountsUrl] nvarchar(200) NOT NULL,
        [ApiBaseUrl] nvarchar(200) NOT NULL,
        [OrganisationId] nvarchar(64) NULL,
        [DepartmentId] nvarchar(64) NULL,
        [ClientId] nvarchar(200) NULL,
        [ClientSecret] nvarchar(1000) NULL,
        [RefreshToken] nvarchar(1000) NULL,
        [Channel] nvarchar(60) NOT NULL,
        [ContactOwnerId] nvarchar(64) NULL,
        [TicketTemplate] nvarchar(max) NULL,
        [GrievanceMatrix] nvarchar(max) NULL,
        [SendAttachments] bit NOT NULL,
        [AlsoSendEmail] bit NOT NULL,
        [LastCheckedAt] datetimeoffset NULL,
        [LastCheckSucceeded] bit NULL,
        [LastCheckResult] nvarchar(1000) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_HelpdeskConnections] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HelpdeskConnections_Partners_PartnerId] FOREIGN KEY ([PartnerId]) REFERENCES [Partners] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911102204_AddHelpdeskAndIntegrations'
)
BEGIN
    CREATE INDEX [IX_ContactMessages_HelpdeskStatus_HelpdeskNextAttemptAt] ON [ContactMessages] ([HelpdeskStatus], [HelpdeskNextAttemptAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911102204_AddHelpdeskAndIntegrations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ExternalIntegrations_Key] ON [ExternalIntegrations] ([Key]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911102204_AddHelpdeskAndIntegrations'
)
BEGIN
    CREATE INDEX [IX_HelpdeskConnections_PartnerId] ON [HelpdeskConnections] ([PartnerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911102204_AddHelpdeskAndIntegrations'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260911102204_AddHelpdeskAndIntegrations', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911112409_AddAgencyMailRelays'
)
BEGIN
    CREATE TABLE [AgencyMailRelays] (
        [Id] int NOT NULL IDENTITY,
        [PartnerId] int NOT NULL,
        [UseOwnServer] bit NOT NULL,
        [Host] nvarchar(200) NULL,
        [Port] int NOT NULL,
        [Encryption] nvarchar(20) NOT NULL,
        [Username] nvarchar(256) NULL,
        [Password] nvarchar(1000) NULL,
        [FromAddress] nvarchar(256) NULL,
        [FromName] nvarchar(200) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_AgencyMailRelays] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AgencyMailRelays_Partners_PartnerId] FOREIGN KEY ([PartnerId]) REFERENCES [Partners] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911112409_AddAgencyMailRelays'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AgencyMailRelays_PartnerId] ON [AgencyMailRelays] ([PartnerId]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911112409_AddAgencyMailRelays'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260911112409_AddAgencyMailRelays', N'10.0.11');
END;

COMMIT;
GO

