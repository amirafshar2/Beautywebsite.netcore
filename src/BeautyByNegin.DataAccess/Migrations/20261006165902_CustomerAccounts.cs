using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeautyByNegin.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CustomerAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatVisitors_SessionTokenHash",
                table: "ChatVisitors");

            migrationBuilder.DropColumn(
                name: "SessionTokenHash",
                table: "ChatVisitors");

            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "Reviews",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAtUtc",
                table: "ChatVisitors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "ChatVisitors",
                type: "TEXT",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "AppointmentRequests",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VisitorId = table.Column<int>(type: "INTEGER", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerSessions_ChatVisitors_VisitorId",
                        column: x => x.VisitorId,
                        principalTable: "ChatVisitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_CustomerId",
                table: "AppointmentRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSessions_TokenHash",
                table: "CustomerSessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSessions_VisitorId",
                table: "CustomerSessions",
                column: "VisitorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerSessions");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_CustomerId",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "LastLoginAtUtc",
                table: "ChatVisitors");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "ChatVisitors");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "AppointmentRequests");

            migrationBuilder.AddColumn<string>(
                name: "SessionTokenHash",
                table: "ChatVisitors",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatVisitors_SessionTokenHash",
                table: "ChatVisitors",
                column: "SessionTokenHash");
        }
    }
}
