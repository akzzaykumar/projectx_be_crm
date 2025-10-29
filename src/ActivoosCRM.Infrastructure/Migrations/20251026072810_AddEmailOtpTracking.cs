using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActivoosCRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailOtpTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmailResendCount",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EmailVerificationAttempts",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEmailResendAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEmailVerificationAttempt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailResendCount",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationAttempts",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastEmailResendAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastEmailVerificationAttempt",
                table: "users");
        }
    }
}
