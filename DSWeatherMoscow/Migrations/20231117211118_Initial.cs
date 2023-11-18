using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DSWeatherMoscow.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeatherData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Temperature = table.Column<double>(type: "double precision", nullable: true),
                    AirHumidity = table.Column<double>(type: "double precision", nullable: true),
                    Td = table.Column<double>(type: "double precision", nullable: true),
                    AtmPressure = table.Column<double>(type: "double precision", nullable: true),
                    AirDirection = table.Column<string>(type: "text", nullable: true),
                    AirSpeed = table.Column<double>(type: "double precision", nullable: true),
                    Cloudiness = table.Column<double>(type: "double precision", nullable: true),
                    H = table.Column<double>(type: "double precision", nullable: true),
                    VV = table.Column<double>(type: "double precision", nullable: true),
                    WeatherEvents = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherData", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeatherData");
        }
    }
}
