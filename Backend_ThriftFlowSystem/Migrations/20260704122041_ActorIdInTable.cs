using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ThriftFlowSystem.Migrations
{
    /// <inheritdoc />
    public partial class ActorIdInTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActorId",
                table: "AuthLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthLogs_ActorId",
                table: "AuthLogs",
                column: "ActorId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthLogs_Employees_ActorId",
                table: "AuthLogs",
                column: "ActorId",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthLogs_Employees_ActorId",
                table: "AuthLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuthLogs_ActorId",
                table: "AuthLogs");

            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "AuthLogs");
        }
    }
}
