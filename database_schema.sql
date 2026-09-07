IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805110316_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [DeletedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805110316_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [FirstName] nvarchar(max) NOT NULL,
        [LastName] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [DeletedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805110316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_RoleId] ON [Users] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805110316_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260805110316_InitialCreate', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810120817_FixDeletedByType'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'DeletedBy');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Users] ALTER COLUMN [DeletedBy] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810120817_FixDeletedByType'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Roles]') AND [c].[name] = N'DeletedBy');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Roles] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Roles] ALTER COLUMN [DeletedBy] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810120817_FixDeletedByType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810120817_FixDeletedByType', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812073453_AddCustomer'
)
BEGIN
    CREATE TABLE [Customer] (
        [Id] uniqueidentifier NOT NULL,
        [FirstName] nvarchar(25) NOT NULL,
        [LastName] nvarchar(25) NOT NULL,
        [IdentityNumber] nvarchar(11) NOT NULL,
        [DateOfBirth] datetime2 NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [PhoneNumber] nvarchar(15) NOT NULL,
        [Address] nvarchar(100) NOT NULL,
        [City] nvarchar(50) NOT NULL,
        [District] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [DeletedBy] uniqueidentifier NULL,
        CONSTRAINT [PK_Customer] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812073453_AddCustomer'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812073453_AddCustomer', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812120347_AddVehicle'
)
BEGIN
    CREATE TABLE [Vehicle] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [PlateNumber] nvarchar(20) NOT NULL,
        [VIN] nvarchar(17) NOT NULL,
        [Brand] nvarchar(50) NOT NULL,
        [Model] nvarchar(50) NOT NULL,
        [ModelYear] int NOT NULL,
        [VehicleType] int NOT NULL,
        [FuelType] int NOT NULL,
        [TransmissionType] int NOT NULL,
        [EngineVolume] decimal(4,2) NULL,
        [EnginePower] int NULL,
        [Color] nvarchar(30) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [DeletedBy] uniqueidentifier NULL,
        CONSTRAINT [PK_Vehicle] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Vehicle_Customer_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customer] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812120347_AddVehicle'
)
BEGIN
    CREATE INDEX [IX_Vehicle_CustomerId] ON [Vehicle] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812120347_AddVehicle'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812120347_AddVehicle', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813111737_AddQuote'
)
BEGIN
    ALTER TABLE [Vehicle] DROP CONSTRAINT [FK_Vehicle_Customer_CustomerId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813111737_AddQuote'
)
BEGIN
    ALTER TABLE [Vehicle] DROP CONSTRAINT [PK_Vehicle];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813111737_AddQuote'
)
BEGIN
    ALTER TABLE [Customer] DROP CONSTRAINT [PK_Customer];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813111737_AddQuote'
)
BEGIN
    EXEC sp_rename N'[Vehicle]', N'Vehicles';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813111737_AddQuote'
)
BEGIN
    EXEC sp_rename N'[Customer]', N'Customers';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813111737_AddQuote'
)
BEGIN
    EXEC sp_rename N'[Vehicles].[IX_Vehicle_CustomerId]', N'IX_Vehicles_CustomerId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813111737_AddQuote'
)
BEGIN
    ALTER TABLE [Vehicles] ADD CONSTRAINT [PK_Vehicles] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813111737_AddQuote'
)
BEGIN
    ALTER TABLE [Customers] ADD CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813111737_AddQuote'
)
BEGIN
    CREATE TABLE [Quotes] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [VehicleId] uniqueidentifier NOT NULL,
        [QuoteNumber] nvarchar(30) NOT NULL,
        [PremiumAmount] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [ValidUntil] datetime2 NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [DeletedBy] uniqueidentifier NULL,
        CONSTRAINT [PK_Quotes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Quotes_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Quotes_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813111737_AddQuote'
)
BEGIN
    CREATE INDEX [IX_Quotes_CustomerId] ON [Quotes] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813111737_AddQuote'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Quotes_QuoteNumber] ON [Quotes] ([QuoteNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813111737_AddQuote'
)
BEGIN
    CREATE INDEX [IX_Quotes_VehicleId] ON [Quotes] ([VehicleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813111737_AddQuote'
)
BEGIN
    ALTER TABLE [Vehicles] ADD CONSTRAINT [FK_Vehicles_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813111737_AddQuote'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260813111737_AddQuote', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813124108_AddPolicy'
)
BEGIN
    CREATE TABLE [Policies] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [VehicleId] uniqueidentifier NOT NULL,
        [QuoteId] uniqueidentifier NOT NULL,
        [PolicyNumber] nvarchar(30) NOT NULL,
        [PremiumAmount] decimal(18,2) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [DeletedBy] uniqueidentifier NULL,
        CONSTRAINT [PK_Policies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Policies_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Policies_Quotes_QuoteId] FOREIGN KEY ([QuoteId]) REFERENCES [Quotes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Policies_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813124108_AddPolicy'
)
BEGIN
    CREATE INDEX [IX_Policies_CustomerId] ON [Policies] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813124108_AddPolicy'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Policies_PolicyNumber] ON [Policies] ([PolicyNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813124108_AddPolicy'
)
BEGIN
    CREATE INDEX [IX_Policies_QuoteId] ON [Policies] ([QuoteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813124108_AddPolicy'
)
BEGIN
    CREATE INDEX [IX_Policies_VehicleId] ON [Policies] ([VehicleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813124108_AddPolicy'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260813124108_AddPolicy', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813141628_AddPayment'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] uniqueidentifier NOT NULL,
        [PolicyId] uniqueidentifier NOT NULL,
        [TransactionNumber] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [PaymentDate] datetime2 NULL,
        [FailureReason] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [DeletedBy] uniqueidentifier NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Policies_PolicyId] FOREIGN KEY ([PolicyId]) REFERENCES [Policies] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813141628_AddPayment'
)
BEGIN
    CREATE INDEX [IX_Payments_PolicyId] ON [Payments] ([PolicyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813141628_AddPayment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260813141628_AddPayment', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814111639_AddPaymentTransactionNumberUniqueIndex'
)
BEGIN
    ALTER TABLE [Payments] DROP CONSTRAINT [FK_Payments_Policies_PolicyId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814111639_AddPaymentTransactionNumberUniqueIndex'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payments]') AND [c].[name] = N'TransactionNumber');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Payments] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Payments] ALTER COLUMN [TransactionNumber] nvarchar(30) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814111639_AddPaymentTransactionNumberUniqueIndex'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Payments_TransactionNumber] ON [Payments] ([TransactionNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814111639_AddPaymentTransactionNumberUniqueIndex'
)
BEGIN
    ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_Policies_PolicyId] FOREIGN KEY ([PolicyId]) REFERENCES [Policies] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814111639_AddPaymentTransactionNumberUniqueIndex'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260814111639_AddPaymentTransactionNumberUniqueIndex', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814121055_AddUniqueActivePolicyPerQuote'
)
BEGIN
    DROP INDEX [IX_Policies_QuoteId] ON [Policies];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814121055_AddUniqueActivePolicyPerQuote'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Policies_QuoteId] ON [Policies] ([QuoteId]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814121055_AddUniqueActivePolicyPerQuote'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260814121055_AddUniqueActivePolicyPerQuote', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814130846_AddVehicleUniqueActiveIndexes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Vehicles_PlateNumber] ON [Vehicles] ([PlateNumber]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814130846_AddVehicleUniqueActiveIndexes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Vehicles_VIN] ON [Vehicles] ([VIN]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814130846_AddVehicleUniqueActiveIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260814130846_AddVehicleUniqueActiveIndexes', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814131449_AddCustomerUniqueActiveIndexes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Customers_Email] ON [Customers] ([Email]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814131449_AddCustomerUniqueActiveIndexes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Customers_IdentityNumber] ON [Customers] ([IdentityNumber]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814131449_AddCustomerUniqueActiveIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260814131449_AddCustomerUniqueActiveIndexes', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817112904_AddPolicyRowVersion'
)
BEGIN
    ALTER TABLE [Policies] ADD [RowVersion] rowversion NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817112904_AddPolicyRowVersion'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260817112904_AddPolicyRowVersion', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827075013_AddCoverage'
)
BEGIN
    CREATE TABLE [Coverages] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [BasePrice] decimal(18,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [DeletedBy] uniqueidentifier NULL,
        CONSTRAINT [PK_Coverages] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827075013_AddCoverage'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Coverages_Name] ON [Coverages] ([Name]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827075013_AddCoverage'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260827075013_AddCoverage', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827081218_AddCoveragePricingFields'
)
BEGIN
    ALTER TABLE [Coverages] ADD [DefaultLimit] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827081218_AddCoveragePricingFields'
)
BEGIN
    ALTER TABLE [Coverages] ADD [IsRequired] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827081218_AddCoveragePricingFields'
)
BEGIN
    ALTER TABLE [Coverages] ADD [PricingType] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827081218_AddCoveragePricingFields'
)
BEGIN
    ALTER TABLE [Coverages] ADD [Rate] decimal(9,4) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827081218_AddCoveragePricingFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260827081218_AddCoveragePricingFields', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827101638_AddVehicleMarketValue'
)
BEGIN
    ALTER TABLE [Vehicles] ADD [MarketValue] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827101638_AddVehicleMarketValue'
)
BEGIN
    CREATE TABLE [QuoteCoverages] (
        [Id] uniqueidentifier NOT NULL,
        [QuoteId] uniqueidentifier NOT NULL,
        [CoverageId] uniqueidentifier NOT NULL,
        [CalculatedPrice] decimal(18,2) NOT NULL,
        [Limit] decimal(18,2) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [DeletedBy] uniqueidentifier NULL,
        CONSTRAINT [PK_QuoteCoverages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuoteCoverages_Coverages_CoverageId] FOREIGN KEY ([CoverageId]) REFERENCES [Coverages] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuoteCoverages_Quotes_QuoteId] FOREIGN KEY ([QuoteId]) REFERENCES [Quotes] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827101638_AddVehicleMarketValue'
)
BEGIN
    CREATE INDEX [IX_QuoteCoverages_CoverageId] ON [QuoteCoverages] ([CoverageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827101638_AddVehicleMarketValue'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_QuoteCoverages_QuoteId_CoverageId] ON [QuoteCoverages] ([QuoteId], [CoverageId]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827101638_AddVehicleMarketValue'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260827101638_AddVehicleMarketValue', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827110554_AddVehicleValueCatalog'
)
BEGIN
    CREATE TABLE [VehicleValueCatalogs] (
        [Id] uniqueidentifier NOT NULL,
        [BrandCode] nvarchar(50) NOT NULL,
        [TypeCode] nvarchar(50) NOT NULL,
        [BrandName] nvarchar(100) NOT NULL,
        [TypeName] nvarchar(500) NOT NULL,
        [ModelYear] int NOT NULL,
        [Value] decimal(18,2) NOT NULL,
        [Source] nvarchar(50) NOT NULL,
        [EffectiveDate] datetime2 NOT NULL,
        [ImportedAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [DeletedBy] uniqueidentifier NULL,
        CONSTRAINT [PK_VehicleValueCatalogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827110554_AddVehicleValueCatalog'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_VehicleValueCatalogs_BrandCode_TypeCode_ModelYear_EffectiveDate] ON [VehicleValueCatalogs] ([BrandCode], [TypeCode], [ModelYear], [EffectiveDate]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827110554_AddVehicleValueCatalog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260827110554_AddVehicleValueCatalog', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828065108_AddPricingRules'
)
BEGIN
    CREATE TABLE [PricingRules] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [Value] decimal(18,4) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [DeletedBy] uniqueidentifier NULL,
        CONSTRAINT [PK_PricingRules] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828065108_AddPricingRules'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PricingRules_Code] ON [PricingRules] ([Code]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828065108_AddPricingRules'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260828065108_AddPricingRules', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828070834_SeedPricingRules'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedBy', N'CreatedDate', N'DeletedBy', N'DeletedDate', N'Description', N'IsActive', N'IsDeleted', N'Name', N'UpdatedBy', N'UpdatedDate', N'Value') AND [object_id] = OBJECT_ID(N'[PricingRules]'))
        SET IDENTITY_INSERT [PricingRules] ON;
    EXEC(N'INSERT INTO [PricingRules] ([Id], [Code], [CreatedBy], [CreatedDate], [DeletedBy], [DeletedDate], [Description], [IsActive], [IsDeleted], [Name], [UpdatedBy], [UpdatedDate], [Value])
    VALUES (''10000000-0000-0000-0000-000000000001'', N''BASE_KASKO_RATE'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, N''Araç değerine uygulanacak temel kasko oranı.'', CAST(1 AS bit), CAST(0 AS bit), N''Temel Kasko Oranı'', NULL, NULL, 0.02),
    (''10000000-0000-0000-0000-000000000002'', N''AGE_0_2'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''0-2 Yaş Katsayısı'', NULL, NULL, 1.0),
    (''10000000-0000-0000-0000-000000000003'', N''AGE_3_5'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''3-5 Yaş Katsayısı'', NULL, NULL, 1.1),
    (''10000000-0000-0000-0000-000000000004'', N''AGE_6_8'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''6-8 Yaş Katsayısı'', NULL, NULL, 1.2),
    (''10000000-0000-0000-0000-000000000005'', N''AGE_9_12'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''9-12 Yaş Katsayısı'', NULL, NULL, 1.35),
    (''10000000-0000-0000-0000-000000000006'', N''AGE_13_15'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''13-15 Yaş Katsayısı'', NULL, NULL, 1.5),
    (''10000000-0000-0000-0000-000000000007'', N''USAGE_PRIVATE'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Özel Kullanım Katsayısı'', NULL, NULL, 1.0),
    (''10000000-0000-0000-0000-000000000008'', N''USAGE_COMMERCIAL'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Ticari Kullanım Katsayısı'', NULL, NULL, 1.25),
    (''10000000-0000-0000-0000-000000000009'', N''USAGE_RENTAL'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Kiralık Kullanım Katsayısı'', NULL, NULL, 1.4),
    (''10000000-0000-0000-0000-000000000010'', N''DRIVER_25_PLUS'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''25+ Yaş Sürücü Katsayısı'', NULL, NULL, 1.0),
    (''10000000-0000-0000-0000-000000000011'', N''DRIVER_21_24'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''21-24 Yaş Sürücü Katsayısı'', NULL, NULL, 1.15),
    (''10000000-0000-0000-0000-000000000012'', N''DRIVER_18_20'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''18-20 Yaş Sürücü Katsayısı'', NULL, NULL, 1.3),
    (''10000000-0000-0000-0000-000000000013'', N''CLAIMS_0'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Hasarsızlık Katsayısı'', NULL, NULL, 0.9),
    (''10000000-0000-0000-0000-000000000014'', N''CLAIMS_1'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''1 Hasar Katsayısı'', NULL, NULL, 1.0),
    (''10000000-0000-0000-0000-000000000015'', N''CLAIMS_2'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''2 Hasar Katsayısı'', NULL, NULL, 1.15),
    (''10000000-0000-0000-0000-000000000016'', N''CLAIMS_3_PLUS'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''3+ Hasar Katsayısı'', NULL, NULL, 1.3),
    (''10000000-0000-0000-0000-000000000017'', N''REGION_LOW'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Düşük Bölge Riski'', NULL, NULL, 0.95),
    (''10000000-0000-0000-0000-000000000018'', N''REGION_NORMAL'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Normal Bölge Riski'', NULL, NULL, 1.0),
    (''10000000-0000-0000-0000-000000000019'', N''REGION_HIGH'', NULL, ''2026-01-01T00:00:00.0000000'', NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Yüksek Bölge Riski'', NULL, NULL, 1.1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedBy', N'CreatedDate', N'DeletedBy', N'DeletedDate', N'Description', N'IsActive', N'IsDeleted', N'Name', N'UpdatedBy', N'UpdatedDate', N'Value') AND [object_id] = OBJECT_ID(N'[PricingRules]'))
        SET IDENTITY_INSERT [PricingRules] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828070834_SeedPricingRules'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260828070834_SeedPricingRules', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828085405_AddVehicleTsbCodes'
)
BEGIN
    ALTER TABLE [Vehicles] ADD [BrandCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828085405_AddVehicleTsbCodes'
)
BEGIN
    ALTER TABLE [Vehicles] ADD [TypeCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828085405_AddVehicleTsbCodes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260828085405_AddVehicleTsbCodes', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828140826_AddInsurancePackages'
)
BEGIN
    CREATE TABLE [InsurancePackages] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Factor] decimal(18,4) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [DeletedBy] uniqueidentifier NULL,
        CONSTRAINT [PK_InsurancePackages] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828140826_AddInsurancePackages'
)
BEGIN
    CREATE TABLE [PackageCoverages] (
        [Id] uniqueidentifier NOT NULL,
        [InsurancePackageId] uniqueidentifier NOT NULL,
        [CoverageId] uniqueidentifier NOT NULL,
        [IsDefault] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [DeletedBy] uniqueidentifier NULL,
        CONSTRAINT [PK_PackageCoverages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PackageCoverages_Coverages_CoverageId] FOREIGN KEY ([CoverageId]) REFERENCES [Coverages] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PackageCoverages_InsurancePackages_InsurancePackageId] FOREIGN KEY ([InsurancePackageId]) REFERENCES [InsurancePackages] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828140826_AddInsurancePackages'
)
BEGIN
    CREATE INDEX [IX_PackageCoverages_CoverageId] ON [PackageCoverages] ([CoverageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828140826_AddInsurancePackages'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PackageCoverages_InsurancePackageId_CoverageId] ON [PackageCoverages] ([InsurancePackageId], [CoverageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828140826_AddInsurancePackages'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260828140826_AddInsurancePackages', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828141258_SeedInsurancePackages'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Name', N'Description', N'Factor', N'IsActive', N'CreatedDate', N'UpdatedDate', N'IsDeleted', N'DeletedDate', N'CreatedBy', N'UpdatedBy', N'DeletedBy') AND [object_id] = OBJECT_ID(N'[InsurancePackages]'))
        SET IDENTITY_INSERT [InsurancePackages] ON;
    EXEC(N'INSERT INTO [InsurancePackages] ([Id], [Code], [Name], [Description], [Factor], [IsActive], [CreatedDate], [UpdatedDate], [IsDeleted], [DeletedDate], [CreatedBy], [UpdatedBy], [DeletedBy])
    VALUES (''11111111-1111-1111-1111-111111111111'', N''EKONOMIK'', N''Ekonomik Paket'', N''Temel kasko paketi'', 1.0, CAST(1 AS bit), ''2026-08-28T00:00:00.0000000Z'', NULL, CAST(0 AS bit), NULL, NULL, NULL, NULL),
    (''22222222-2222-2222-2222-222222222222'', N''STANDART'', N''Standart Paket'', N''Genişletilmiş kasko paketi'', 1.0, CAST(1 AS bit), ''2026-08-28T00:00:00.0000000Z'', NULL, CAST(0 AS bit), NULL, NULL, NULL, NULL),
    (''33333333-3333-3333-3333-333333333333'', N''KAPSAMLI'', N''Kapsamlı Paket'', N''Geniş kapsamlı kasko paketi'', 1.0, CAST(1 AS bit), ''2026-08-28T00:00:00.0000000Z'', NULL, CAST(0 AS bit), NULL, NULL, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Name', N'Description', N'Factor', N'IsActive', N'CreatedDate', N'UpdatedDate', N'IsDeleted', N'DeletedDate', N'CreatedBy', N'UpdatedBy', N'DeletedBy') AND [object_id] = OBJECT_ID(N'[InsurancePackages]'))
        SET IDENTITY_INSERT [InsurancePackages] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828141258_SeedInsurancePackages'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260828141258_SeedInsurancePackages', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828141832_SeedPackageCoverages'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'InsurancePackageId', N'CoverageId', N'IsDefault', N'CreatedDate', N'UpdatedDate', N'IsDeleted', N'DeletedDate', N'CreatedBy', N'UpdatedBy', N'DeletedBy') AND [object_id] = OBJECT_ID(N'[PackageCoverages]'))
        SET IDENTITY_INSERT [PackageCoverages] ON;
    EXEC(N'INSERT INTO [PackageCoverages] ([Id], [InsurancePackageId], [CoverageId], [IsDefault], [CreatedDate], [UpdatedDate], [IsDeleted], [DeletedDate], [CreatedBy], [UpdatedBy], [DeletedBy])
    VALUES (''41111111-1111-1111-1111-111111111111'', ''11111111-1111-1111-1111-111111111111'', ''dd1b1cc2-8b4f-43aa-8ed6-81bfe49200df'', CAST(1 AS bit), ''2026-08-28T00:00:00.0000000Z'', NULL, CAST(0 AS bit), NULL, NULL, NULL, NULL),
    (''41111111-1111-1111-1111-111111111112'', ''11111111-1111-1111-1111-111111111111'', ''a0dd1498-c060-4547-8fa4-9d5cc54c9bf0'', CAST(1 AS bit), ''2026-08-28T00:00:00.0000000Z'', NULL, CAST(0 AS bit), NULL, NULL, NULL, NULL),
    (''42222222-2222-2222-2222-222222222221'', ''22222222-2222-2222-2222-222222222222'', ''dd1b1cc2-8b4f-43aa-8ed6-81bfe49200df'', CAST(1 AS bit), ''2026-08-28T00:00:00.0000000Z'', NULL, CAST(0 AS bit), NULL, NULL, NULL, NULL),
    (''42222222-2222-2222-2222-222222222222'', ''22222222-2222-2222-2222-222222222222'', ''a0dd1498-c060-4547-8fa4-9d5cc54c9bf0'', CAST(1 AS bit), ''2026-08-28T00:00:00.0000000Z'', NULL, CAST(0 AS bit), NULL, NULL, NULL, NULL),
    (''42222222-2222-2222-2222-222222222223'', ''22222222-2222-2222-2222-222222222222'', ''7733cfde-16a2-4329-b613-52a2f5cc8f1b'', CAST(1 AS bit), ''2026-08-28T00:00:00.0000000Z'', NULL, CAST(0 AS bit), NULL, NULL, NULL, NULL),
    (''43333333-3333-3333-3333-333333333331'', ''33333333-3333-3333-3333-333333333333'', ''dd1b1cc2-8b4f-43aa-8ed6-81bfe49200df'', CAST(1 AS bit), ''2026-08-28T00:00:00.0000000Z'', NULL, CAST(0 AS bit), NULL, NULL, NULL, NULL),
    (''43333333-3333-3333-3333-333333333332'', ''33333333-3333-3333-3333-333333333333'', ''a0dd1498-c060-4547-8fa4-9d5cc54c9bf0'', CAST(1 AS bit), ''2026-08-28T00:00:00.0000000Z'', NULL, CAST(0 AS bit), NULL, NULL, NULL, NULL),
    (''43333333-3333-3333-3333-333333333333'', ''33333333-3333-3333-3333-333333333333'', ''7733cfde-16a2-4329-b613-52a2f5cc8f1b'', CAST(1 AS bit), ''2026-08-28T00:00:00.0000000Z'', NULL, CAST(0 AS bit), NULL, NULL, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'InsurancePackageId', N'CoverageId', N'IsDefault', N'CreatedDate', N'UpdatedDate', N'IsDeleted', N'DeletedDate', N'CreatedBy', N'UpdatedBy', N'DeletedBy') AND [object_id] = OBJECT_ID(N'[PackageCoverages]'))
        SET IDENTITY_INSERT [PackageCoverages] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828141832_SeedPackageCoverages'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260828141832_SeedPackageCoverages', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831075835_AddPreviousPolicy'
)
BEGIN
    CREATE TABLE [PreviousPolicies] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [PreviousInsurer] nvarchar(max) NOT NULL,
        [PolicyNumber] nvarchar(max) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [ClaimsCount] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [DeletedBy] uniqueidentifier NULL,
        CONSTRAINT [PK_PreviousPolicies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PreviousPolicies_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831075835_AddPreviousPolicy'
)
BEGIN
    CREATE INDEX [IX_PreviousPolicies_CustomerId] ON [PreviousPolicies] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831075835_AddPreviousPolicy'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831075835_AddPreviousPolicy', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831122914_AddQuotePricingSnapshot'
)
BEGIN
    CREATE TABLE [QuotePricingSnapshots] (
        [Id] uniqueidentifier NOT NULL,
        [QuoteId] uniqueidentifier NOT NULL,
        [MarketValue] decimal(18,2) NOT NULL,
        [BaseRate] decimal(18,6) NOT NULL,
        [AgeFactor] decimal(18,6) NOT NULL,
        [UsageFactor] decimal(18,6) NOT NULL,
        [DriverFactor] decimal(18,6) NOT NULL,
        [ClaimsFactor] decimal(18,6) NOT NULL,
        [RegionFactor] decimal(18,6) NOT NULL,
        [PackageFactor] decimal(18,6) NOT NULL,
        [DeductibleFactor] decimal(18,6) NOT NULL,
        [CoveragePremium] decimal(18,2) NOT NULL,
        [Discount] decimal(18,2) NOT NULL,
        [FinalPremium] decimal(18,2) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [DeletedBy] uniqueidentifier NULL,
        CONSTRAINT [PK_QuotePricingSnapshots] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuotePricingSnapshots_Quotes_QuoteId] FOREIGN KEY ([QuoteId]) REFERENCES [Quotes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831122914_AddQuotePricingSnapshot'
)
BEGIN
    CREATE UNIQUE INDEX [IX_QuotePricingSnapshots_QuoteId] ON [QuotePricingSnapshots] ([QuoteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831122914_AddQuotePricingSnapshot'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831122914_AddQuotePricingSnapshot', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    ALTER TABLE [PricingRules] ADD [EffectiveFrom] datetime2 NOT NULL DEFAULT '2026-01-01T00:00:00.0000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    ALTER TABLE [PricingRules] ADD [EffectiveUntil] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    ALTER TABLE [PricingRules] ADD [Version] int NOT NULL DEFAULT 1;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000002'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000003'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000004'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000005'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000006'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000007'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000008'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000009'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000010'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000011'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000012'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000013'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000014'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000015'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000016'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000017'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000018'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [EffectiveUntil] = NULL, [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000019'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901065527_AddPricingRuleVersioning'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260901065527_AddPricingRuleVersioning', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901070956_AddPricingRuleVersionIndex'
)
BEGIN
    DROP INDEX [IX_PricingRules_Code] ON [PricingRules];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901070956_AddPricingRuleVersionIndex'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PricingRules_Code_Version] ON [PricingRules] ([Code], [Version]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901070956_AddPricingRuleVersionIndex'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260901070956_AddPricingRuleVersionIndex', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000002'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000003'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000004'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000005'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000006'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000007'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000008'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000009'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000010'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000011'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000012'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000013'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000014'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000015'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000016'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000017'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000018'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveFrom] = ''2026-01-01T00:00:00.0000000'', [Version] = 1
    WHERE [Id] = ''10000000-0000-0000-0000-000000000019'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901071937_SyncPricingRuleVersionSeed'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260901071937_SyncPricingRuleVersionSeed', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901072440_AddBaseKaskoRateVersion2'
)
BEGIN
    EXEC(N'UPDATE [PricingRules] SET [EffectiveUntil] = ''2026-08-31T00:00:00.0000000''
    WHERE [Id] = ''10000000-0000-0000-0000-000000000001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901072440_AddBaseKaskoRateVersion2'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedBy', N'CreatedDate', N'DeletedBy', N'DeletedDate', N'Description', N'EffectiveFrom', N'EffectiveUntil', N'IsActive', N'IsDeleted', N'Name', N'UpdatedBy', N'UpdatedDate', N'Value', N'Version') AND [object_id] = OBJECT_ID(N'[PricingRules]'))
        SET IDENTITY_INSERT [PricingRules] ON;
    EXEC(N'INSERT INTO [PricingRules] ([Id], [Code], [CreatedBy], [CreatedDate], [DeletedBy], [DeletedDate], [Description], [EffectiveFrom], [EffectiveUntil], [IsActive], [IsDeleted], [Name], [UpdatedBy], [UpdatedDate], [Value], [Version])
    VALUES (''10000000-0000-0000-0000-000000000020'', N''BASE_KASKO_RATE'', NULL, ''2026-09-01T00:00:00.0000000'', NULL, NULL, N''01.09.2026 itibarıyla geçerli yeni temel kasko oranı.'', ''2026-09-01T00:00:00.0000000'', NULL, CAST(1 AS bit), CAST(0 AS bit), N''Temel Kasko Oranı V2'', NULL, NULL, 0.0215, 2)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedBy', N'CreatedDate', N'DeletedBy', N'DeletedDate', N'Description', N'EffectiveFrom', N'EffectiveUntil', N'IsActive', N'IsDeleted', N'Name', N'UpdatedBy', N'UpdatedDate', N'Value', N'Version') AND [object_id] = OBJECT_ID(N'[PricingRules]'))
        SET IDENTITY_INSERT [PricingRules] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901072440_AddBaseKaskoRateVersion2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260901072440_AddBaseKaskoRateVersion2', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901110448_AddPricingRuleChangeRequest'
)
BEGIN
    CREATE TABLE [PricingRuleChangeRequests] (
        [Id] uniqueidentifier NOT NULL,
        [PricingRuleId] uniqueidentifier NOT NULL,
        [OldValue] decimal(18,2) NOT NULL,
        [NewValue] decimal(18,2) NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [RequestedBy] uniqueidentifier NOT NULL,
        [RequestedDate] datetime2 NOT NULL,
        [ApprovedBy] uniqueidentifier NULL,
        [ApprovedDate] datetime2 NULL,
        [Status] nvarchar(max) NOT NULL,
        [EffectiveFrom] datetime2 NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [DeletedBy] uniqueidentifier NULL,
        CONSTRAINT [PK_PricingRuleChangeRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PricingRuleChangeRequests_PricingRules_PricingRuleId] FOREIGN KEY ([PricingRuleId]) REFERENCES [PricingRules] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PricingRuleChangeRequests_Users_ApprovedBy] FOREIGN KEY ([ApprovedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PricingRuleChangeRequests_Users_RequestedBy] FOREIGN KEY ([RequestedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901110448_AddPricingRuleChangeRequest'
)
BEGIN
    CREATE INDEX [IX_PricingRuleChangeRequests_ApprovedBy] ON [PricingRuleChangeRequests] ([ApprovedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901110448_AddPricingRuleChangeRequest'
)
BEGIN
    CREATE INDEX [IX_PricingRuleChangeRequests_PricingRuleId] ON [PricingRuleChangeRequests] ([PricingRuleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901110448_AddPricingRuleChangeRequest'
)
BEGIN
    CREATE INDEX [IX_PricingRuleChangeRequests_RequestedBy] ON [PricingRuleChangeRequests] ([RequestedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901110448_AddPricingRuleChangeRequest'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260901110448_AddPricingRuleChangeRequest', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901112436_FixPricingRuleChangeRequestPrecision'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PricingRuleChangeRequests]') AND [c].[name] = N'OldValue');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [PricingRuleChangeRequests] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [PricingRuleChangeRequests] ALTER COLUMN [OldValue] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901112436_FixPricingRuleChangeRequestPrecision'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PricingRuleChangeRequests]') AND [c].[name] = N'NewValue');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [PricingRuleChangeRequests] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [PricingRuleChangeRequests] ALTER COLUMN [NewValue] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901112436_FixPricingRuleChangeRequestPrecision'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260901112436_FixPricingRuleChangeRequestPrecision', N'8.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903121044_AddUserCustomerRelation'
)
BEGIN
    ALTER TABLE [Users] ADD [CustomerId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903121044_AddUserCustomerRelation'
)
BEGIN
    CREATE INDEX [IX_Users_CustomerId] ON [Users] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903121044_AddUserCustomerRelation'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903121044_AddUserCustomerRelation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260903121044_AddUserCustomerRelation', N'8.0.20');
END;
GO

COMMIT;
GO

