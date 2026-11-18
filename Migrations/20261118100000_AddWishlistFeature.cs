using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOOK.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "YeuThichs",
                columns: table => new
                {
                    MaYeuThich = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTK = table.Column<int>(type: "int", nullable: false),
                    MaSach = table.Column<int>(type: "int", nullable: false),
                    NgayThem = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuThichs", x => x.MaYeuThich);
                    table.ForeignKey(
                        name: "FK_YeuThichs_Sachs_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sachs",
                        principalColumn: "MaSach",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_YeuThichs_TaiKhoans_MaTK",
                        column: x => x.MaTK,
                        principalTable: "TaiKhoans",
                        principalColumn: "MaTK",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YeuThichs_MaSach",
                table: "YeuThichs",
                column: "MaSach");

            migrationBuilder.CreateIndex(
                name: "IX_YeuThichs_MaTK_MaSach",
                table: "YeuThichs",
                columns: new[] { "MaTK", "MaSach" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YeuThichs");
        }
    }
}
