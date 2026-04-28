using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertificateApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStudiedDatesColumnTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StudiedFrom",
                table: "CertificateOfAttendance",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)");

            migrationBuilder.AlterColumn<string>(
                name: "StudiedTo",
                table: "CertificateOfAttendance",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StudiedFrom",
                table: "CertificateOfAttendance",
                type: "character varying(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "StudiedTo",
                table: "CertificateOfAttendance",
                type: "character varying(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
