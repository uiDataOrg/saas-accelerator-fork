using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Marketplace.SaaS.Accelerator.DataAccess.Migrations;

/// <inheritdoc />
public partial class Add_DataCentralPurchases_Table : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DataCentralPurchases",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EnvironmentName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                TypeOfPurchase = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false), 

                TenantId = table.Column<int>(type: "int", nullable: true),
                EnvironmentId = table.Column<int>(type: "int", nullable: true)
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
