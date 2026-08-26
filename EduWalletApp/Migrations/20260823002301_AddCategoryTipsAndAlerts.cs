using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduWalletApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryTipsAndAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlertKey",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetCategoryId",
                table: "FinancialTips",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ThresholdPercentage",
                table: "FinancialTips",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TriggerType",
                table: "FinancialTips",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTips_TargetCategoryId",
                table: "FinancialTips",
                column: "TargetCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialTips_Categories_TargetCategoryId",
                table: "FinancialTips",
                column: "TargetCategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinancialTips_Categories_TargetCategoryId",
                table: "FinancialTips");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTips_TargetCategoryId",
                table: "FinancialTips");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "AlertKey",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TargetCategoryId",
                table: "FinancialTips");

            migrationBuilder.DropColumn(
                name: "ThresholdPercentage",
                table: "FinancialTips");

            migrationBuilder.DropColumn(
                name: "TriggerType",
                table: "FinancialTips");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Categories");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
