using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSubProjectAndPrefixToProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Add SubProject column if it doesn't exist
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Projects]') AND name = 'SubProject')
                BEGIN
                    ALTER TABLE [dbo].[Projects]
                    ADD [SubProject] NVARCHAR(100) NOT NULL DEFAULT 'MAIN';
                END

                -- Add Prefix column if it doesn't exist
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Projects]') AND name = 'Prefix')
                BEGIN
                    ALTER TABLE [dbo].[Projects]
                    ADD [Prefix] NVARCHAR(50) NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Remove Prefix column if it exists
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Projects]') AND name = 'Prefix')
                BEGIN
                    ALTER TABLE [dbo].[Projects]
                    DROP COLUMN [Prefix];
                END

                -- Remove SubProject column if it exists
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Projects]') AND name = 'SubProject')
                BEGIN
                    ALTER TABLE [dbo].[Projects]
                    DROP COLUMN [SubProject];
                END
            ");
        }
    }
}

