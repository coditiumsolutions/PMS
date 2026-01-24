using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Projects]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [Projects] (
                        [Id] int NOT NULL IDENTITY,
                        [ProjectName] nvarchar(100) NOT NULL,
                        [ProjectDescription] nvarchar(500) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_Projects] PRIMARY KEY ([Id])
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
