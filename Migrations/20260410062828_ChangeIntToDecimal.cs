using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wpf_projekt.Migrations
{
    public partial class ChangeIntToDecimal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Zmiana typu w tabeli PersonalAccounts
            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "PersonalAccounts",
                type: "TEXT", // SQLite przechowuje decimal jako TEXT lub REAL
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            // Zmiana typu w tabeli SharedAccounts
            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "SharedAccounts",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            // Zmiana typu w tabeli Users
            migrationBuilder.AlterColumn<decimal>(
                name: "Earnings",
                table: "Users",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Powrót do typu int (może wiązać się z utratą precyzji po przecinku!)
            migrationBuilder.AlterColumn<int>(
                name: "Balance",
                table: "PersonalAccounts",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "Balance",
                table: "SharedAccounts",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "Earnings",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");
        }
    }
}