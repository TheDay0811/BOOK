using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOOK.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NhaXuatBans",
                columns: table => new
                {
                    MaNXB = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenNXB = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhaXuatBans", x => x.MaNXB);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoans",
                columns: table => new
                {
                    MaTK = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoans", x => x.MaTK);
                });

            migrationBuilder.CreateTable(
                name: "TheLoais",
                columns: table => new
                {
                    MaTheLoai = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenTheLoai = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TheLoais", x => x.MaTheLoai);
                });

            migrationBuilder.CreateTable(
                name: "TheThuViens",
                columns: table => new
                {
                    MaThe = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoThe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayHetHan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TinhTrangSuDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaTK = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TheThuViens", x => x.MaThe);
                    table.ForeignKey(
                        name: "FK_TheThuViens_TaiKhoans_MaTK",
                        column: x => x.MaTK,
                        principalTable: "TaiKhoans",
                        principalColumn: "MaTK",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sachs",
                columns: table => new
                {
                    MaSach = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaSachCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenSach = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TacGia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NamXuatBan = table.Column<int>(type: "int", nullable: false),
                    MaTheLoai = table.Column<int>(type: "int", nullable: false),
                    TheLoaiMaTheLoai = table.Column<int>(type: "int", nullable: false),
                    MaNXB = table.Column<int>(type: "int", nullable: false),
                    NhaXuatBanMaNXB = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sachs", x => x.MaSach);
                    table.ForeignKey(
                        name: "FK_Sachs_NhaXuatBans_NhaXuatBanMaNXB",
                        column: x => x.NhaXuatBanMaNXB,
                        principalTable: "NhaXuatBans",
                        principalColumn: "MaNXB",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sachs_TheLoais_TheLoaiMaTheLoai",
                        column: x => x.TheLoaiMaTheLoai,
                        principalTable: "TheLoais",
                        principalColumn: "MaTheLoai",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhieuMuons",
                columns: table => new
                {
                    MaPhieuMuon = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NgayMuon = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HanTra = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaThe = table.Column<int>(type: "int", nullable: false),
                    TheMaThe = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuMuons", x => x.MaPhieuMuon);
                    table.ForeignKey(
                        name: "FK_PhieuMuons_TheThuViens_TheMaThe",
                        column: x => x.TheMaThe,
                        principalTable: "TheThuViens",
                        principalColumn: "MaThe",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DauSachs",
                columns: table => new
                {
                    MaDauSach = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoKiemSoat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TinhTrang = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaSach = table.Column<int>(type: "int", nullable: false),
                    SachMaSach = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DauSachs", x => x.MaDauSach);
                    table.ForeignKey(
                        name: "FK_DauSachs_Sachs_SachMaSach",
                        column: x => x.SachMaSach,
                        principalTable: "Sachs",
                        principalColumn: "MaSach",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietPhieuMuons",
                columns: table => new
                {
                    MaChiTiet = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NgayTraThucTe = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TinhTrangTra = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TienBoiThuong = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DaBoiThuong = table.Column<bool>(type: "bit", nullable: false),
                    MaPhieuMuon = table.Column<int>(type: "int", nullable: false),
                    PhieuMuonMaPhieuMuon = table.Column<int>(type: "int", nullable: false),
                    MaDauSach = table.Column<int>(type: "int", nullable: false),
                    DauSachMaDauSach = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietPhieuMuons", x => x.MaChiTiet);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuMuons_DauSachs_DauSachMaDauSach",
                        column: x => x.DauSachMaDauSach,
                        principalTable: "DauSachs",
                        principalColumn: "MaDauSach",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuMuons_PhieuMuons_PhieuMuonMaPhieuMuon",
                        column: x => x.PhieuMuonMaPhieuMuon,
                        principalTable: "PhieuMuons",
                        principalColumn: "MaPhieuMuon",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuMuons_DauSachMaDauSach",
                table: "ChiTietPhieuMuons",
                column: "DauSachMaDauSach");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuMuons_PhieuMuonMaPhieuMuon",
                table: "ChiTietPhieuMuons",
                column: "PhieuMuonMaPhieuMuon");

            migrationBuilder.CreateIndex(
                name: "IX_DauSachs_SachMaSach",
                table: "DauSachs",
                column: "SachMaSach");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuMuons_TheMaThe",
                table: "PhieuMuons",
                column: "TheMaThe");

            migrationBuilder.CreateIndex(
                name: "IX_Sachs_NhaXuatBanMaNXB",
                table: "Sachs",
                column: "NhaXuatBanMaNXB");

            migrationBuilder.CreateIndex(
                name: "IX_Sachs_TheLoaiMaTheLoai",
                table: "Sachs",
                column: "TheLoaiMaTheLoai");

            migrationBuilder.CreateIndex(
                name: "IX_TheThuViens_MaTK",
                table: "TheThuViens",
                column: "MaTK",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietPhieuMuons");

            migrationBuilder.DropTable(
                name: "DauSachs");

            migrationBuilder.DropTable(
                name: "PhieuMuons");

            migrationBuilder.DropTable(
                name: "Sachs");

            migrationBuilder.DropTable(
                name: "TheThuViens");

            migrationBuilder.DropTable(
                name: "NhaXuatBans");

            migrationBuilder.DropTable(
                name: "TheLoais");

            migrationBuilder.DropTable(
                name: "TaiKhoans");
        }
    }
}
