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


        migrationBuilder.Sql(@$"
            INSERT INTO ApplicationConfiguration
	            ([Name],[Value],[Description])
            VALUES
	            ('DataCentralEditionId_P1','1','Id of edition in DataCentral P1 tenants are assigned to')
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
           name: "DataCentralTenants");

        migrationBuilder.Sql(@$"
            DELETE FROM ApplicationConfiguration
            WHERE [Name] = 'DataCentralEditionId_P1'
        ");

    }
}
