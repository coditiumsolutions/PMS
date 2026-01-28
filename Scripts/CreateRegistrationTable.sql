-- Create Registration Table
-- This script creates the Registration table if it doesn't exist

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Registration]') AND type in (N'U'))
BEGIN
    CREATE TABLE [Registration] (
        [RegID] INT NOT NULL IDENTITY(1,1),
        [FullName] NVARCHAR(150) NULL,
        [CNIC] NVARCHAR(50) NULL,
        [Phone] NVARCHAR(50) NULL,
        [Email] NVARCHAR(150) NULL,
        [ProjectID] INT NULL,
        [RequestedSize] NVARCHAR(100) NULL,
        [Remarks] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME NULL DEFAULT GETDATE(),
        [Status] NVARCHAR(50) NULL DEFAULT 'Pending',
        CONSTRAINT [PK_Registration] PRIMARY KEY ([RegID])
    );
    
    -- Create indexes for better query performance
    CREATE INDEX [IX_Registration_ProjectID] ON [Registration] ([ProjectID]);
    CREATE INDEX [IX_Registration_Status] ON [Registration] ([Status]);
    CREATE INDEX [IX_Registration_CreatedAt] ON [Registration] ([CreatedAt]);
    
    PRINT 'Registration table created successfully.';
END
ELSE
BEGIN
    PRINT 'Registration table already exists.';
END
