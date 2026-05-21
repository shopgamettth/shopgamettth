using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coffee.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTransferCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransferCode",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Users_TransferCode",
                table: "Users",
                column: "TransferCode",
                unique: true,
                filter: "[TransferCode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Users_TransferCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TransferCode",
                table: "Users");
        }
    }
}
