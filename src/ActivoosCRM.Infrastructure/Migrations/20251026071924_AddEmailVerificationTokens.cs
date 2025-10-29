using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActivoosCRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_activity_providers_locations_LocationId",
                table: "activity_providers");

            migrationBuilder.DropForeignKey(
                name: "FK_activity_providers_users_UserId1",
                table: "activity_providers");

            migrationBuilder.DropIndex(
                name: "IX_activity_providers_UserId1",
                table: "activity_providers");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "activity_providers");

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationToken",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationTokenExpiry",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_activity_providers_locations_LocationId",
                table: "activity_providers",
                column: "LocationId",
                principalTable: "locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_activity_providers_locations_LocationId",
                table: "activity_providers");

            migrationBuilder.DropColumn(
                name: "EmailVerificationToken",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationTokenExpiry",
                table: "users");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "activity_providers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_activity_providers_UserId1",
                table: "activity_providers",
                column: "UserId1",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_activity_providers_locations_LocationId",
                table: "activity_providers",
                column: "LocationId",
                principalTable: "locations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_activity_providers_users_UserId1",
                table: "activity_providers",
                column: "UserId1",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}
