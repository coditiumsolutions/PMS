-- =============================================
-- Script: Create CustomerAuditLog Table
-- Description: Creates a table to store audit logs for Customer record updates and deletions
-- Author: PMS System
-- Date: 2025-01-08
-- =============================================

-- Check if table exists, drop if needed (optional - comment out if you want to preserve existing data)
-- IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CustomerAuditLog]') AND type in (N'U'))
-- BEGIN
--     DROP TABLE [dbo].[CustomerAuditLog];
-- END
-- GO

-- Create CustomerAuditLog table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CustomerAuditLog]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CustomerAuditLog] (
        [LogID] INT NOT NULL IDENTITY(1,1),
        [CustomerID] NVARCHAR(50) NOT NULL,
        [ActionType] NVARCHAR(20) NOT NULL,
        [ChangedFields] NVARCHAR(MAX) NULL,
        [OldValues] NVARCHAR(MAX) NULL,
        [NewValues] NVARCHAR(MAX) NULL,
        [ActionBy] NVARCHAR(50) NOT NULL,
        [ActionDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [Remarks] NVARCHAR(500) NULL,
        CONSTRAINT [PK_CustomerAuditLog] PRIMARY KEY ([LogID])
    );
    
    PRINT 'Table CustomerAuditLog created successfully.';
END
ELSE
BEGIN
    PRINT 'Table CustomerAuditLog already exists.';
END
GO

-- Create index on CustomerID for faster lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CustomerAuditLog_CustomerID' AND object_id = OBJECT_ID('dbo.CustomerAuditLog'))
BEGIN
    CREATE INDEX [IX_CustomerAuditLog_CustomerID] 
    ON [dbo].[CustomerAuditLog] ([CustomerID]);
    PRINT 'Index IX_CustomerAuditLog_CustomerID created successfully.';
END
ELSE
BEGIN
    PRINT 'Index IX_CustomerAuditLog_CustomerID already exists.';
END
GO

-- Create index on ActionDate for faster date-based queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CustomerAuditLog_ActionDate' AND object_id = OBJECT_ID('dbo.CustomerAuditLog'))
BEGIN
    CREATE INDEX [IX_CustomerAuditLog_ActionDate] 
    ON [dbo].[CustomerAuditLog] ([ActionDate]);
    PRINT 'Index IX_CustomerAuditLog_ActionDate created successfully.';
END
ELSE
BEGIN
    PRINT 'Index IX_CustomerAuditLog_ActionDate already exists.';
END
GO

-- Create composite index on CustomerID and ActionDate for optimized filtering
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CustomerAuditLog_CustomerID_ActionDate' AND object_id = OBJECT_ID('dbo.CustomerAuditLog'))
BEGIN
    CREATE INDEX [IX_CustomerAuditLog_CustomerID_ActionDate] 
    ON [dbo].[CustomerAuditLog] ([CustomerID], [ActionDate] DESC);
    PRINT 'Index IX_CustomerAuditLog_CustomerID_ActionDate created successfully.';
END
ELSE
BEGIN
    PRINT 'Index IX_CustomerAuditLog_CustomerID_ActionDate already exists.';
END
GO

-- Add check constraint to ensure ActionType is either 'UPDATE' or 'DELETE'
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_CustomerAuditLog_ActionType')
BEGIN
    ALTER TABLE [dbo].[CustomerAuditLog]
    ADD CONSTRAINT [CK_CustomerAuditLog_ActionType] 
    CHECK ([ActionType] IN ('UPDATE', 'DELETE'));
    PRINT 'Check constraint CK_CustomerAuditLog_ActionType created successfully.';
END
ELSE
BEGIN
    PRINT 'Check constraint CK_CustomerAuditLog_ActionType already exists.';
END
GO

PRINT 'CustomerAuditLog table setup completed successfully.';
GO

-- =============================================
-- Example Queries:
-- =============================================

-- View all audit logs
-- SELECT * FROM [dbo].[CustomerAuditLog] ORDER BY [ActionDate] DESC;

-- View audit logs for a specific customer
-- SELECT * FROM [dbo].[CustomerAuditLog] WHERE [CustomerID] = 'CUST001' ORDER BY [ActionDate] DESC;

-- View all DELETE operations
-- SELECT * FROM [dbo].[CustomerAuditLog] WHERE [ActionType] = 'DELETE' ORDER BY [ActionDate] DESC;

-- View all UPDATE operations
-- SELECT * FROM [dbo].[CustomerAuditLog] WHERE [ActionType] = 'UPDATE' ORDER BY [ActionDate] DESC;

-- View audit logs for a date range
-- SELECT * FROM [dbo].[CustomerAuditLog] 
-- WHERE [ActionDate] BETWEEN '2025-01-01' AND '2025-01-31' 
-- ORDER BY [ActionDate] DESC;

