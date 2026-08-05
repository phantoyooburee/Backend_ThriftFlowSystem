using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ThriftFlowSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "POSShiftId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LocationDetails = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoreName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TaxId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReceiptFooter = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "POSShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartingCash = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExpectedCash = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ActualCash = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Difference = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POSShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_POSShifts_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_POSShifts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Branches",
                columns: new[] { "Id", "BranchName", "IsActive", "LocationDetails" },
                values: new object[] { 1, "Main Store", true, null });

            migrationBuilder.InsertData(
                table: "StoreProfiles",
                columns: new[] { "Id", "Address", "Phone", "ReceiptFooter", "StoreName", "TaxId" },
                values: new object[] { 1, "Thailand", "", "Thank you for shopping!", "THRIFT FLOW", "" });

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_EmployeeId",
                table: "Refunds",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BranchId",
                table: "Orders",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_POSShiftId",
                table: "Orders",
                column: "POSShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_POSShifts_BranchId",
                table: "POSShifts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_POSShifts_EmployeeId",
                table: "POSShifts",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Branches_BranchId",
                table: "Orders",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_POSShifts_POSShiftId",
                table: "Orders",
                column: "POSShiftId",
                principalTable: "POSShifts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Refunds_Employees_EmployeeId",
                table: "Refunds",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Branches_BranchId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_POSShifts_POSShiftId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Refunds_Employees_EmployeeId",
                table: "Refunds");

            migrationBuilder.DropTable(
                name: "POSShifts");

            migrationBuilder.DropTable(
                name: "StoreProfiles");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_Refunds_EmployeeId",
                table: "Refunds");

            migrationBuilder.DropIndex(
                name: "IX_Orders_BranchId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_POSShiftId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "POSShiftId",
                table: "Orders");
        }
    }
}
