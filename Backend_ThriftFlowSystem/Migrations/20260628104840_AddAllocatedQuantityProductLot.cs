using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ThriftFlowSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAllocatedQuantityProductLot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AllocatedQuantity",
                table: "ProductLots",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllocatedQuantity",
                table: "ProductLots");
        }
    }
}
