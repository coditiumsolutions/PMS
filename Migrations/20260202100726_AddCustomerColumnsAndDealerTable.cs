using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerColumnsAndDealerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: only add Dealer table and new Customer columns (DB already has other tables)
            migrationBuilder.Sql(@"
                -- Dealer table
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Dealer')
                BEGIN
                    CREATE TABLE [dbo].[Dealer] (
                        [DealerID] INT IDENTITY(1,1) NOT NULL,
                        [DealerCode] NVARCHAR(50) NULL,
                        [DealerName] NVARCHAR(100) NOT NULL,
                        [CompanyName] NVARCHAR(150) NULL,
                        [ContactNo] NVARCHAR(100) NULL,
                        [Email] NVARCHAR(100) NULL,
                        [Address] NVARCHAR(255) NULL,
                        [City] NVARCHAR(60) NULL,
                        [IsActive] BIT NOT NULL DEFAULT 1,
                        [CreatedAt] DATETIME NULL DEFAULT GETDATE(),
                        [CreatedBy] NVARCHAR(100) NULL,
                        [Remarks] NVARCHAR(500) NULL,
                        CONSTRAINT [PK_Dealer] PRIMARY KEY ([DealerID])
                    );
                    CREATE INDEX [IX_Dealer_DealerCode] ON [dbo].[Dealer] ([DealerCode]) WHERE [DealerCode] IS NOT NULL;
                END

                -- Customers: PlanNo, CustomerImage, CustomerAttachment, DealerID
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = 'PlanNo')
                    ALTER TABLE [dbo].[Customers] ADD [PlanNo] NVARCHAR(100) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = 'CustomerImage')
                    ALTER TABLE [dbo].[Customers] ADD [CustomerImage] NVARCHAR(500) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = 'CustomerAttachment')
                    ALTER TABLE [dbo].[Customers] ADD [CustomerAttachment] NVARCHAR(MAX) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = 'DealerID')
                BEGIN
                    ALTER TABLE [dbo].[Customers] ADD [DealerID] INT NULL;
                    ALTER TABLE [dbo].[Customers] ADD CONSTRAINT [FK_Customers_Dealer_DealerID] FOREIGN KEY ([DealerID]) REFERENCES [dbo].[Dealer] ([DealerID]) ON DELETE SET NULL;
                    CREATE INDEX [IX_Customers_DealerID] ON [dbo].[Customers] ([DealerID]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = 'DealerID')
                BEGIN
                    ALTER TABLE [dbo].[Customers] DROP CONSTRAINT [FK_Customers_Dealer_DealerID];
                    DROP INDEX [IX_Customers_DealerID] ON [dbo].[Customers];
                    ALTER TABLE [dbo].[Customers] DROP COLUMN [DealerID];
                END
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = 'CustomerAttachment')
                    ALTER TABLE [dbo].[Customers] DROP COLUMN [CustomerAttachment];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = 'CustomerImage')
                    ALTER TABLE [dbo].[Customers] DROP COLUMN [CustomerImage];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = 'PlanNo')
                    ALTER TABLE [dbo].[Customers] DROP COLUMN [PlanNo];
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Dealer')
                    DROP TABLE [dbo].[Dealer];
            ");
        }
    }
}
