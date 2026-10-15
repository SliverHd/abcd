using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.XepLich;
using XepLichGiangDay.Api.Models.Entities;
using XepLichGiangDay.Api.Services.RangBuoc;

namespace XepLichGiangDay.Api.Services.ThuatToan.Genetic;

public class GeneticAlgorithmService(
    AppDbContext context,
    BoDanhGiaPhuongAn boDanhGia,
    TaoDanhSachUngVien taoUngVien) : ISchedulingAlgorithm
{
    public string TenThuatToan => "GA";

    public async Task<KetQuaXepLichDto> ChayAsync(DuLieuXepLichDto duLieu, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        // 1. Nạp ngữ cảnh dữ liệu học kỳ vào bộ nhớ đệm (chỉ nạp 1 lần duy nhất từ CSDL)
        var boiCanh = await boDanhGia.TaoBoiCanhAsync(duLieu.MaHocKy, cancellationToken);
        if (boiCanh.DanhSachLop.Count == 0)
        {
            return new KetQuaXepLichDto
            {
                ThuatToan = TenThuatToan,
                TrangThai = "KhongCoLopHocPhan",
                TongDiemPhat = 0
            };
        }

        // 2. Lấy danh sách lớp đã khóa (ghim cố định bởi Trưởng bộ môn)
        var dsPhanCongHienTai = await context.PhanCongGiangDay
            .Where(x => x.MaHocKy == duLieu.MaHocKy)
            .ToListAsync(cancellationToken);

        var phanCongKhoa = dsPhanCongHienTai
            .Where(x => x.TrangThai == "DaKhoa")
            .ToDictionary(x => x.MaLopHocPhan, x => x.MaGiangVien);
        var cacLopCoDinh = phanCongKhoa.Keys.ToHashSet();

        // 3. Tạo danh sách ứng viên chuyên môn cho từng lớp học phần
        var dsNangLuc = await context.NangLucGiangDay
            .Where(x => string.IsNullOrEmpty(x.TrangThai) || x.TrangThai == "DangApDung")
            .ToListAsync(cancellationToken);

        var ungVienTheoLop = taoUngVien.TaoToanBo(
            boiCanh.DanhSachLop,
            boiCanh.ChiTietTheoLop.Values.SelectMany(x => x).ToList(),
            dsNangLuc,
            boiCanh.LichBanTheoGv.Values.SelectMany(x => x).ToList(),
            boiCanh.GiangVienTheoMa.Values.ToList());

        // 4. Chuẩn bị bảng thời gian các lớp phục vụ toán tử sửa lỗi
        var thoiGianTheoLop = boiCanh.ChiTietTheoLop.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Select(ct => new ThoiGianLop
            {
                MaLopHocPhan = ct.MaLopHocPhan,
                Thu = ct.Thu,
                TietBatDau = ct.TietBatDau,
                TietKetThuc = ct.TietKetThuc,
                TuTuan = ct.TuTuan > 0 ? ct.TuTuan : 1,
                DenTuan = ct.DenTuan > 0 ? ct.DenTuan : 53
            }).ToList()
        );

        // 5. Cấu hình tham số tiến hóa GA
        int popSize = duLieu.KichThuocQuanThe.GetValueOrDefault(50);
        int maxGenerations = duLieu.SoTheHe.GetValueOrDefault(100);
        double crossoverRate = duLieu.TyLeLaiGhep.GetValueOrDefault(0.8);
        double mutationRate = duLieu.TyLeDotBien.GetValueOrDefault(0.05);

        if (popSize < 10) popSize = 10;
        if (maxGenerations < 1) maxGenerations = 10;

        var khoiTao = new KhoiTaoQuanThe();
        var chonLoc = new ChonLoc();
        var laiGhep = new LaiGhep();
        var dotBien = new DotBien();
        var suaCaThe = new SuaCaThe();

        var danhSachMaLop = boiCanh.DanhSachLop.Select(x => x.MaLopHocPhan).ToList();

        // 6. Khởi tạo quần thể ban đầu
        var quanThe = khoiTao.TaoQuanTheBanDau(popSize, danhSachMaLop, ungVienTheoLop, phanCongKhoa);

        void DanhGiaQuanThe(List<CaThe> danhSach)
        {
            foreach (var ct in danhSach)
            {
                var dictPhanCong = ct.DanhSachGen.ToDictionary(g => g.MaLopHocPhan, g => (int?)g.MaGiangVien);
                var kq = boDanhGia.DanhGia(dictPhanCong, boiCanh);
                ct.DiemPhat = kq.TongDiemPhat;
                ct.KetQuaDanhGia = kq;
            }
        }

        // Đánh giá quần thể thế hệ 0
        DanhGiaQuanThe(quanThe);
        quanThe = quanThe.OrderBy(c => c.DiemPhat).ToList();

        var bestOverall = SaoChepCaThe(quanThe[0]);
        var lichSuDiemPhat = new List<double> { bestOverall.DiemPhat };

        int theHeThucTe = 0;

        // 7. Vòng lặp tiến hóa qua các thế hệ
        for (int gen = 1; gen <= maxGenerations; gen++)
        {
            theHeThucTe = gen;
            cancellationToken.ThrowIfCancellationRequested();

            // Nếu đã tìm thấy phương án tối ưu tuyệt đối (điểm phạt = 0) thì dừng sớm
            if (bestOverall.DiemPhat <= 0.0001)
            {
                break;
            }

            var theHeMoi = new List<CaThe>();

            // Elitism: Giữ lại 2 cá thể tốt nhất thế hệ trước
            theHeMoi.Add(SaoChepCaThe(quanThe[0]));
            if (quanThe.Count > 1)
            {
                theHeMoi.Add(SaoChepCaThe(quanThe[1]));
            }

            // Sinh các cá thể mới bằng Lai ghép + Đột biến + Sửa lỗi
            while (theHeMoi.Count < popSize)
            {
                var cha = chonLoc.ChonCaTheChaMe(quanThe, 3);
                var me = chonLoc.ChonCaTheChaMe(quanThe, 3);

                var con = laiGhep.TaoCon(cha, me, crossoverRate, cacLopCoDinh);
                con = dotBien.ThucHien(con, mutationRate, ungVienTheoLop, cacLopCoDinh);
                con = suaCaThe.SuaNeuViPham(con, thoiGianTheoLop, ungVienTheoLop, cacLopCoDinh);

                theHeMoi.Add(con);
            }

            // Đánh giá thế hệ mới và sắp xếp
            DanhGiaQuanThe(theHeMoi);
            quanThe = theHeMoi.OrderBy(c => c.DiemPhat).ToList();

            // Cập nhật cá thể tốt nhất toàn cục
            if (quanThe[0].DiemPhat < bestOverall.DiemPhat)
            {
                bestOverall = SaoChepCaThe(quanThe[0]);
            }

            lichSuDiemPhat.Add(bestOverall.DiemPhat);
        }

        sw.Stop();

        // 8. Lưu kết quả tốt nhất vào CSDL (bảng phan_cong_giang_day và ket_qua_xep_lich)
        using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var dsPhanCongDto = new List<PhanCongDto>();

            // Cập nhật bảng phan_cong_giang_day
            foreach (var gen in bestOverall.DanhSachGen)
            {
                if (!gen.MaGiangVien.HasValue || gen.MaGiangVien.Value <= 0) continue;

                var pcTonTai = dsPhanCongHienTai.FirstOrDefault(x => x.MaLopHocPhan == gen.MaLopHocPhan);
                if (pcTonTai != null)
                {
                    // Chỉ cập nhật nếu lớp chưa bị khóa ghim
                    if (pcTonTai.TrangThai != "DaKhoa")
                    {
                        pcTonTai.MaGiangVien = gen.MaGiangVien.Value;
                        pcTonTai.NguonPhanCong = "GA";
                        pcTonTai.TrangThai = "DuKien";
                    }
                }
                else
                {
                    pcTonTai = new PhanCongGiangDay
                    {
                        MaHocKy = duLieu.MaHocKy,
                        MaLopHocPhan = gen.MaLopHocPhan,
                        MaGiangVien = gen.MaGiangVien.Value,
                        NguonPhanCong = "GA",
                        TrangThai = "DuKien"
                    };
                    context.PhanCongGiangDay.Add(pcTonTai);
                }

                dsPhanCongDto.Add(new PhanCongDto
                {
                    MaLopHocPhan = gen.MaLopHocPhan,
                    MaGiangVien = gen.MaGiangVien.Value,
                    DiemPhat = 0
                });
            }

            // Ghi nhật ký vào bảng ket_qua_xep_lich
            var oldKetQua = await context.KetQuaXepLich
                .Where(x => x.MaHocKy == duLieu.MaHocKy)
                .ToListAsync(cancellationToken);
            context.KetQuaXepLich.RemoveRange(oldKetQua);

            foreach (var gen in bestOverall.DanhSachGen)
            {
                if (gen.MaGiangVien.HasValue && gen.MaGiangVien.Value > 0)
                {
                    context.KetQuaXepLich.Add(new KetQuaXepLich
                    {
                        MaHocKy = duLieu.MaHocKy,
                        MaLopHocPhan = gen.MaLopHocPhan,
                        MaGiangVien = gen.MaGiangVien.Value,
                        DiemPhat = 0,
                        GhiChu = $"GA thế hệ {theHeThucTe}"
                    });
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var kqChiTiet = bestOverall.KetQuaDanhGia;
            int soViPhamCung = kqChiTiet != null
                ? (kqChiTiet.SoViPhamChuyenMon + kqChiTiet.SoViPhamTrungLich + kqChiTiet.SoViPhamLichBan + kqChiTiet.SoLopChuaPhanCong)
                : 0;

            string trangThaiKetQua = bestOverall.DiemPhat <= 0.0001
                ? "ToiUuTuyetDoi"
                : (soViPhamCung == 0 ? "ThoaManRangBuocCung" : "ConViPham");

            return new KetQuaXepLichDto
            {
                ThuatToan = TenThuatToan,
                TongDiemPhat = bestOverall.DiemPhat,
                DoThichNghi = kqChiTiet?.DoThichNghi ?? (1.0 / (1.0 + bestOverall.DiemPhat)),
                DiemPhatRangBuocCung = kqChiTiet?.DiemPhatRangBuocCung ?? 0,
                DiemPhatNguyenVong = kqChiTiet?.DiemPhatNguyenVong ?? 0,
                DiemPhatCanBangTai = kqChiTiet?.DiemPhatCanBangTai ?? 0,
                DiemPhatGomLich = kqChiTiet?.DiemPhatGomLich ?? 0,
                SoViPhamCung = soViPhamCung,
                SoLopChuaPhanCong = kqChiTiet?.SoLopChuaPhanCong ?? 0,
                SoTheHeThucTe = theHeThucTe,
                ThoiGianChayMiligiay = sw.ElapsedMilliseconds,
                TrangThai = trangThaiKetQua,
                LichSuDiemPhatTheoTheHe = lichSuDiemPhat,
                DanhSachViPham = kqChiTiet?.DanhSachViPham ?? [],
                DanhSachPhanCong = dsPhanCongDto
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static CaThe SaoChepCaThe(CaThe goc)
    {
        return new CaThe
        {
            DiemPhat = goc.DiemPhat,
            KetQuaDanhGia = goc.KetQuaDanhGia,
            DanhSachGen = goc.DanhSachGen.Select(g => new Gen
            {
                MaLopHocPhan = g.MaLopHocPhan,
                MaGiangVien = g.MaGiangVien
            }).ToList()
        };
    }
}
