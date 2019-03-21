using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StockTraderBroker.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BuyRequests",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<Guid>(nullable: false),
                    StockId = table.Column<long>(nullable: false),
                    Price = table.Column<double>(nullable: false),
                    TimeOut = table.Column<DateTime>(nullable: false),
                    AmountOfShares = table.Column<int>(nullable: false),
                    ReserveId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuyRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SellRequests",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<Guid>(nullable: false),
                    StockId = table.Column<long>(nullable: false),
                    Price = table.Column<double>(nullable: false),
                    TimeOut = table.Column<DateTime>(nullable: false),
                    AmountOfShares = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellRequests", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuyRequests");

            migrationBuilder.DropTable(
                name: "SellRequests");
        }
    }
}
