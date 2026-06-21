using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ThriftFlowSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemActionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "InventoryLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Prefix",
                table: "Categories",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "SystemActionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetTable = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetRecordId = table.Column<int>(type: "integer", nullable: true),
                    Details = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemActionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemActionLogs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLogs_EmployeeId",
                table: "InventoryLogs",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemActionLogs_EmployeeId",
                table: "SystemActionLogs",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryLogs_Employees_EmployeeId",
                table: "InventoryLogs",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryLogs_Employees_EmployeeId",
                table: "InventoryLogs");

            migrationBuilder.DropTable(
                name: "SystemActionLogs");

            migrationBuilder.DropIndex(
                name: "IX_InventoryLogs_EmployeeId",
                table: "InventoryLogs");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "InventoryLogs");

            migrationBuilder.DropColumn(
                name: "Prefix",
                table: "Categories");
        }
    }
}
