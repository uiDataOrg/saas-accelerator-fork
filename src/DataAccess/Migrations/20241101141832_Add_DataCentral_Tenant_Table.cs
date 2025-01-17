using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Marketplace.SaaS.Accelerator.DataAccess.Migrations;

/// <inheritdoc />
public partial class Add_DataCentral_Tenant_Table : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DataCentralTenants",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                TenantId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DataCentralTenants", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
           name: "DataCentralTenants");
    }
}
