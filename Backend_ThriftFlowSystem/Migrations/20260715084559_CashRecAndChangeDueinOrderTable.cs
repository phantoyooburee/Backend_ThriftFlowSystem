using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ThriftFlowSystem.Migrations
{
    /// <inheritdoc />
    public partial class CashRecAndChangeDueinOrderTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CashReceived",
                table: "Orders",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ChangeDue",
                table: "Orders",
                type: "numeric(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashReceived",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ChangeDue",
                table: "Orders");
        }
    }
}
