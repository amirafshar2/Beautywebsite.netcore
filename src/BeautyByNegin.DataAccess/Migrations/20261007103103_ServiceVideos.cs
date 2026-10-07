using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeautyByNegin.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ServiceVideos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VideoId",
                table: "Services",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MediaVideos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StorageKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationSeconds = table.Column<double>(type: "REAL", nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    OriginalSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: true),
                    NotOptimized = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasPoster = table.Column<bool>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaVideos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Services_VideoId",
                table: "Services",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaVideos_StorageKey",
                table: "MediaVideos",
                column: "StorageKey",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_MediaVideos_VideoId",
                table: "Services",
                column: "VideoId",
                principalTable: "MediaVideos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Services_MediaVideos_VideoId",
                table: "Services");

            migrationBuilder.DropTable(
                name: "MediaVideos");

            migrationBuilder.DropIndex(
                name: "IX_Services_VideoId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "VideoId",
                table: "Services");
        }
    }
}
