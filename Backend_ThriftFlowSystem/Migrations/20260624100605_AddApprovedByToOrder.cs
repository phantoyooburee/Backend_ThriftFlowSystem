using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ThriftFlowSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovedByToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovedById",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ApprovedById",
                table: "Orders",
                column: "ApprovedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Employees_ApprovedById",
                table: "Orders",
                column: "ApprovedById",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Employees_ApprovedById",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ApprovedById",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "Orders");
        }
    }
}
