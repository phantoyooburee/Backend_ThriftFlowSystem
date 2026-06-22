using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ThriftFlowSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexToInvitationToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InvitedByEmployeeId",
                table: "EmployeeInvitations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvitations_InvitationToken",
                table: "EmployeeInvitations",
                column: "InvitationToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvitations_InvitedByEmployeeId",
                table: "EmployeeInvitations",
                column: "InvitedByEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeInvitations_Employees_InvitedByEmployeeId",
                table: "EmployeeInvitations",
                column: "InvitedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeInvitations_Employees_InvitedByEmployeeId",
                table: "EmployeeInvitations");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeInvitations_InvitationToken",
                table: "EmployeeInvitations");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeInvitations_InvitedByEmployeeId",
                table: "EmployeeInvitations");

            migrationBuilder.DropColumn(
                name: "InvitedByEmployeeId",
                table: "EmployeeInvitations");
        }
    }
}
