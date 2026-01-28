-- Add RegistrationID column to Customers table
-- This column links a Customer to a Registration record

IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') 
    AND name = 'RegistrationID'
)
BEGIN
    ALTER TABLE [Customers]
    ADD [RegistrationID] INT NULL;
    
    -- Create index for better query performance
    CREATE INDEX [IX_Customers_RegistrationID] ON [Customers] ([RegistrationID]);
    
    PRINT 'RegistrationID column added to Customers table successfully.';
END
ELSE
BEGIN
    PRINT 'RegistrationID column already exists in Customers table.';
END
