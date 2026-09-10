using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduWalletApp.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringFinanceDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "IncomeSources",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "IncomeSources",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "Expenses",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "Expenses",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "IncomeSources");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "IncomeSources");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Expenses");
        }
    }
}
