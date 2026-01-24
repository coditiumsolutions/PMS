using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerAuditLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CustomerAuditLog]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [CustomerAuditLog] (
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
                    
                    -- Create indexes for better query performance
                    CREATE INDEX [IX_CustomerAuditLog_CustomerID] ON [CustomerAuditLog] ([CustomerID]);
                    CREATE INDEX [IX_CustomerAuditLog_ActionDate] ON [CustomerAuditLog] ([ActionDate]);
                    CREATE INDEX [IX_CustomerAuditLog_CustomerID_ActionDate] ON [CustomerAuditLog] ([CustomerID], [ActionDate]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CustomerAuditLog]') AND type in (N'U'))
                BEGIN
                    DROP TABLE [CustomerAuditLog];
                END
            ");
        }
    }
}

