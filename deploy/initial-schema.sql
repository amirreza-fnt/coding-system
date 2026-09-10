-- Request Coding Service — initial schema (offline server, no dotnet-ef)
-- Run in SSMS on database: apiweb-codingsystem

IF OBJECT_ID(N'[dbo].[TrackingRequests]', N'U') IS NULL
BEGIN
    CREATE TABLE [Systems] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(32) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Systems] PRIMARY KEY ([Id])
    );

    CREATE TABLE [RequestCounters] (
        [Id] bigint NOT NULL IDENTITY,
        [SystemId] int NOT NULL,
        [NationalCode] nvarchar(10) NOT NULL,
        [Counter] int NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_RequestCounters] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RequestCounters_Systems_SystemId] FOREIGN KEY ([SystemId]) REFERENCES [Systems] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [TrackingRequests] (
        [Id] uniqueidentifier NOT NULL,
        [SystemId] int NOT NULL,
        [Counter] int NOT NULL,
        [NationalCode] nvarchar(10) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(150) NOT NULL,
        [Mobile] nvarchar(15) NULL,
        [Landline] nvarchar(20) NULL,
        [Description] nvarchar(4000) NULL,
        [Status] nvarchar(16) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_TrackingRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TrackingRequests_Systems_SystemId] FOREIGN KEY ([SystemId]) REFERENCES [Systems] ([Id]) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [IX_Systems_Code] ON [Systems] ([Code]);
    CREATE UNIQUE INDEX [IX_RequestCounters_SystemId_NationalCode] ON [RequestCounters] ([SystemId], [NationalCode]);
    CREATE INDEX [IX_TrackingRequests_CreatedAtUtc] ON [TrackingRequests] ([CreatedAtUtc]);
    CREATE INDEX [IX_TrackingRequests_Landline] ON [TrackingRequests] ([Landline]);
    CREATE INDEX [IX_TrackingRequests_Mobile] ON [TrackingRequests] ([Mobile]);
    CREATE INDEX [IX_TrackingRequests_NationalCode] ON [TrackingRequests] ([NationalCode]);
    CREATE UNIQUE INDEX [IX_TrackingRequests_SystemId_NationalCode_Counter] ON [TrackingRequests] ([SystemId], [NationalCode], [Counter]);

    SET IDENTITY_INSERT [Systems] ON;
    INSERT INTO [Systems] ([Id], [Code], [CreatedAtUtc], [IsActive], [Name]) VALUES
    (1, N'137', '2026-09-09T00:00:00.0000000', 1, N'سامانه ۱۳۷'),
    (2, N'FIRE', '2026-09-09T00:00:00.0000000', 1, N'آتش‌نشانی');
    SET IDENTITY_INSERT [Systems] OFF;

    IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL
    BEGIN
        CREATE TABLE [__EFMigrationsHistory] (
            [MigrationId] nvarchar(150) NOT NULL,
            [ProductVersion] nvarchar(32) NOT NULL,
            CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
        );
    END;

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260909061648_InitialCreate', N'8.0.11');
END
GO
