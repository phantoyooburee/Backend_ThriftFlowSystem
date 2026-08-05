using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ThriftFlowSystem.Migrations
{
    /// <inheritdoc />
    public partial class ApprovedByFKIdRefundTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Refunds_ApprovedById",
                table: "Refunds",
                column: "ApprovedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Refunds_Employees_ApprovedById",
                table: "Refunds",
                column: "ApprovedById",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Refunds_Employees_ApprovedById",
                table: "Refunds");

            migrationBuilder.DropIndex(
                name: "IX_Refunds_ApprovedById",
                table: "Refunds");
        }
    }
}
