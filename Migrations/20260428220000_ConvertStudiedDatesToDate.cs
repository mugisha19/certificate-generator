using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertificateApp.Migrations
{
    /// <inheritdoc />
    public partial class ConvertStudiedDatesToDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert StudiedFrom: parse free-form text like "January 2026" into a date.
            // Rows that fail to parse fall back to NULL — and StudiedFrom becomes nullable
            // for the duration of the conversion, then the column type changes.
            migrationBuilder.Sql(@"
                ALTER TABLE ""CertificateOfAttendance""
                ALTER COLUMN ""StudiedFrom"" DROP NOT NULL;

                ALTER TABLE ""CertificateOfAttendance""
                ALTER COLUMN ""StudiedFrom"" TYPE date
                USING (
                    CASE
                        WHEN ""StudiedFrom"" IS NULL OR btrim(""StudiedFrom"") = '' THEN NULL
                        ELSE (
                            CASE
                                WHEN ""StudiedFrom"" ~ '^[A-Za-z]+ +[0-9]{4}$'
                                    THEN to_date(""StudiedFrom"", 'FMMonth YYYY')
                                WHEN ""StudiedFrom"" ~ '^[0-9]{4}-[0-9]{2}-[0-9]{2}$'
                                    THEN ""StudiedFrom""::date
                                ELSE NULL
                            END
                        )
                    END
                );

                ALTER TABLE ""CertificateOfAttendance""
                ALTER COLUMN ""StudiedTo"" TYPE date
                USING (
                    CASE
                        WHEN ""StudiedTo"" IS NULL OR btrim(""StudiedTo"") = '' THEN NULL
                        WHEN lower(btrim(""StudiedTo"")) = 'date' THEN NULL
                        ELSE (
                            CASE
                                WHEN ""StudiedTo"" ~ '^[A-Za-z]+ +[0-9]{4}$'
                                    THEN to_date(""StudiedTo"", 'FMMonth YYYY')
                                WHEN ""StudiedTo"" ~ '^[0-9]{4}-[0-9]{2}-[0-9]{2}$'
                                    THEN ""StudiedTo""::date
                                ELSE NULL
                            END
                        )
                    END
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""CertificateOfAttendance""
                ALTER COLUMN ""StudiedFrom"" TYPE text
                USING to_char(""StudiedFrom"", 'FMMonth YYYY');

                ALTER TABLE ""CertificateOfAttendance""
                ALTER COLUMN ""StudiedTo"" TYPE text
                USING to_char(""StudiedTo"", 'FMMonth YYYY');
            ");
        }
    }
}
