using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhistleblowerPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKeypairSaltAndIv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "PublicKey",
                table: "Investigators",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "EncryptedPrivateKey",
                table: "Investigators",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");

            migrationBuilder.AddColumn<byte[]>(
                name: "PrivateKeyIv",
                table: "Investigators",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PrivateKeySalt",
                table: "Investigators",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrivateKeyIv",
                table: "Investigators");

            migrationBuilder.DropColumn(
                name: "PrivateKeySalt",
                table: "Investigators");

            migrationBuilder.AlterColumn<byte[]>(
                name: "PublicKey",
                table: "Investigators",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "EncryptedPrivateKey",
                table: "Investigators",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)",
                oldNullable: true);
        }
    }
}
