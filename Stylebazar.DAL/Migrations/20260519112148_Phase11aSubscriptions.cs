using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stylebazar.DAL.Migrations
{
    /// <inheritdoc />
    public partial class Phase11aSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentPlan",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlanExpiresAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Plan = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByAdminId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_AspNetUsers_ApprovedByAdminId",
                        column: x => x.ApprovedByAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Subscriptions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ApprovedByAdminId",
                table: "Subscriptions",
                column: "ApprovedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscriptions",
                column: "UserId",
                unique: true,
                filter: "[Status] = 0 AND [DeletedDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId_CreatedDate",
                table: "Subscriptions",
                columns: new[] { "UserId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId_Status",
                table: "Subscriptions",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CurrentPlan",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PlanExpiresAt",
                table: "AspNetUsers");
        }
    }
}
