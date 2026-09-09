using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RequestCodingService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Systems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Systems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequestCounters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SystemId = table.Column<int>(type: "int", nullable: false),
                    NationalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Counter = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestCounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestCounters_Systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "Systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackingRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemId = table.Column<int>(type: "int", nullable: false),
                    Counter = table.Column<int>(type: "int", nullable: false),
                    NationalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Landline = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackingRequests_Systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "Systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Systems",
                columns: new[] { "Id", "Code", "CreatedAtUtc", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "137", new DateTime(2026, 9, 9, 0, 0, 0, 0, DateTimeKind.Utc), true, "سامانه ۱۳۷" },
                    { 2, "FIRE", new DateTime(2026, 9, 9, 0, 0, 0, 0, DateTimeKind.Utc), true, "آتش‌نشانی" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestCounters_SystemId_NationalCode",
                table: "RequestCounters",
                columns: new[] { "SystemId", "NationalCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Systems_Code",
                table: "Systems",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackingRequests_CreatedAtUtc",
                table: "TrackingRequests",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingRequests_Landline",
                table: "TrackingRequests",
                column: "Landline");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingRequests_Mobile",
                table: "TrackingRequests",
                column: "Mobile");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingRequests_NationalCode",
                table: "TrackingRequests",
                column: "NationalCode");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingRequests_SystemId_NationalCode_Counter",
                table: "TrackingRequests",
                columns: new[] { "SystemId", "NationalCode", "Counter" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestCounters");

            migrationBuilder.DropTable(
                name: "TrackingRequests");

            migrationBuilder.DropTable(
                name: "Systems");
        }
    }
}
