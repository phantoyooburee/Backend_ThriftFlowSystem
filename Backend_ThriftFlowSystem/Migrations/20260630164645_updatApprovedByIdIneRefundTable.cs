using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ThriftFlowSystem.Migrations
{
    /// <inheritdoc />
    public partial class updatApprovedByIdIneRefundTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovedById",
                table: "Refunds",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "Refunds");
        }
    }
}
