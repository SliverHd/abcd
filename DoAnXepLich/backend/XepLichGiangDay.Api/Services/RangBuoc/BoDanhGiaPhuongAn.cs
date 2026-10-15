using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.Models.Entities;

namespace XepLichGiangDay.Api.Services.RangBuoc;

/// <summary>
/// Ngữ cảnh dữ liệu được index sẵn để đánh giá phương án siêu tốc trong vòng lặp tiến hóa GA.
/// </summary>
public class BoiCanhDanhGia
{
    public int MaHocKy { get; set; }
    public List<LopHocPhan> DanhSachLop { get; set; } = [];
    public Dictionary<int, LopHocPhan> LopTheoMa { get; set; } = [];
    public Dictionary<int, List<ChiTietThoiKhoaBieu>> ChiTietTheoLop { get; set; } = [];
    public Dictionary<int, GiangVien> GiangVienTheoMa { get; set; } = [];
    public Dictionary<int, HashSet<int>> NangLucTheoGv { get; set; } = []; // MaGiangVien -> Set<MaHocPhan>
    public Dictionary<int, List<LichBanGiangVien>> LichBanTheoGv { get; set; } = []; // MaGiangVien -> List<LichBan>
    public Dictionary<int, List<NguyenVongGiangVien>> NguyenVongTheoGv { get; set; } = []; // MaGiangVien -> List<NguyenVong>
    public Dictionary<int, DinhMucGiangVien> DinhMucTheoGv { get; set; } = []; // MaGiangVien -> DinhMuc
    public Dictionary<string, double> TrongSoRangBuoc { get; set; } = [];

    // Trọng số các tiêu chí (lấy từ cấu hình bảng rang_buoc_hoc_ky hoặc mặc định chuẩn)
    public double TrongSoChuyenMon => LayTrongSo("Sai chuyên môn", 1000.0);
    public double TrongSoTrungLich => LayTrongSo("Trùng lịch dạy", 1000.0);
    public double TrongSoLichBan => LayTrongSo("Vi phạm lịch bận cố định", 1000.0);
    public double TrongSoPhuLop => LayTrongSo("Phủ kín các lớp học phần", 1000.0);
    public double TrongSoCanBangTai => LayTrongSo("Lệch định mức giờ dạy", 15.0);
    public double TrongSoNguyenVong => LayTrongSo("Không thỏa mãn nguyện vọng", 10.0);
    public double TrongSoGomLich => LayTrongSo("Rải lịch dạy trong tuần", 25.0);
    public double TrongSoCoHuu => LayTrongSo("Ưu tiên giảng viên cơ hữu", 5.0);

    private double LayTrongSo(string ten, double macDinh)
    {
        foreach (var (k, v) in TrongSoRangBuoc)
        {
            if (k.Contains(ten, StringComparison.OrdinalIgnoreCase) || ten.Contains(k, StringComparison.OrdinalIgnoreCase))
            {
                return v;
            }
        }
        return macDinh;
    }
}

public class KetQuaDanhGiaPhuongAn
{
    public double TongDiemPhat { get; set; }
    public double DoThichNghi { get; set; } // Fitness = 1.0 / (1.0 + TongDiemPhat)
    public double DiemPhatRangBuocCung { get; set; }
    public double DiemPhatNguyenVong { get; set; }
    public double DiemPhatCanBangTai { get; set; }
    public double DiemPhatGomLich { get; set; }
    public int SoLopChuaPhanCong { get; set; }
    public int SoViPhamChuyenMon { get; set; }
    public int SoViPhamTrungLich { get; set; }
    public int SoViPhamLichBan { get; set; }
    public bool HopLe => SoLopChuaPhanCong == 0 && SoViPhamChuyenMon == 0 && SoViPhamTrungLich == 0 && SoViPhamLichBan == 0;
    public List<string> DanhSachViPham { get; set; } = [];
}

public class BoDanhGiaPhuongAn
{
    private readonly AppDbContext? _context;
    private readonly BoKiemTraRangBuoc _boKiemTra;

    public BoDanhGiaPhuongAn()
    {
        _boKiemTra = new BoKiemTraRangBuoc();
    }

    public BoDanhGiaPhuongAn(AppDbContext context, BoKiemTraRangBuoc boKiemTra)
    {
        _context = context;
        _boKiemTra = boKiemTra;
    }

    /// <summary>
    /// Đánh giá toàn diện phương án phân công (tính điểm phạt ràng buộc cứng + mềm, xuất ra Fitness).
    /// </summary>
    public KetQuaDanhGiaPhuongAn DanhGia(IDictionary<int, int?> phanCong, BoiCanhDanhGia boiCanh)
    {
        var ketQua = new KetQuaDanhGiaPhuongAn();

        // 1. Kiểm tra RÀNG BUỘC CỨNG (Hard Constraints)
        // 1.1 Phủ kín lớp
        foreach (var lop in boiCanh.DanhSachLop)
        {
            if (!phanCong.TryGetValue(lop.MaLopHocPhan, out var maGv) || !maGv.HasValue || maGv.Value <= 0)
            {
                ketQua.SoLopChuaPhanCong++;
                ketQua.DiemPhatRangBuocCung += boiCanh.TrongSoPhuLop;
                ketQua.DanhSachViPham.Add($"Lớp [{lop.MaLopHocPhanTruong}] chưa được phân công giảng viên.");
            }
        }

        // 1.2 Đúng chuyên môn
        foreach (var (maLop, maGvOpt) in phanCong)
        {
            if (!maGvOpt.HasValue || maGvOpt.Value <= 0) continue;
            int maGv = maGvOpt.Value;

            if (boiCanh.LopTheoMa.TryGetValue(maLop, out var lop))
            {
                bool dungChuyenMon = boiCanh.NangLucTheoGv.TryGetValue(maGv, out var setHp) && setHp.Contains(lop.MaHocPhan);
                if (!dungChuyenMon)
                {
                    ketQua.SoViPhamChuyenMon++;
                    ketQua.DiemPhatRangBuocCung += boiCanh.TrongSoChuyenMon;
                    string tenGv = boiCanh.GiangVienTheoMa.TryGetValue(maGv, out var gv) ? gv.HoTen : $"GV #{maGv}";
                    ketQua.DanhSachViPham.Add($"{tenGv} không có chuyên môn dạy lớp [{lop.MaLopHocPhanTruong}].");
                }
            }
        }

        // Nhóm các lớp theo từng giảng viên được phân công
        var lopTheoGv = phanCong
            .Where(x => x.Value.HasValue && x.Value.Value > 0)
            .GroupBy(x => x.Value!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Key).ToList());

        // 1.3 Trùng lịch và vướng lịch bận cố định
        foreach (var (maGv, dsLopCuaGv) in lopTheoGv)
        {
            string tenGv = boiCanh.GiangVienTheoMa.TryGetValue(maGv, out var gv) ? gv.HoTen : $"GV #{maGv}";

            var cacBuoiHocCuaGv = new List<ChiTietThoiKhoaBieu>();
            foreach (var maLop in dsLopCuaGv)
            {
                if (boiCanh.ChiTietTheoLop.TryGetValue(maLop, out var dsCt))
                {
                    cacBuoiHocCuaGv.AddRange(dsCt);
                }
            }

            // Kiểm tra trùng lịch giữa 2 lớp khác nhau
            for (int i = 0; i < cacBuoiHocCuaGv.Count; i++)
            {
                for (int j = i + 1; j < cacBuoiHocCuaGv.Count; j++)
                {
                    var ct1 = cacBuoiHocCuaGv[i];
                    var ct2 = cacBuoiHocCuaGv[j];

                    if (ct1.MaLopHocPhan != ct2.MaLopHocPhan && _boKiemTra.CoTrungLich(
                        ct1.Thu, ct1.TietBatDau, ct1.TietKetThuc, ct1.TuTuan, ct1.DenTuan,
                        ct2.Thu, ct2.TietBatDau, ct2.TietKetThuc, ct2.TuTuan, ct2.DenTuan))
                    {
                        ketQua.SoViPhamTrungLich++;
                        ketQua.DiemPhatRangBuocCung += boiCanh.TrongSoTrungLich;
                        ketQua.DanhSachViPham.Add($"{tenGv} bị trùng giờ dạy giữa 2 lớp vào Thứ {ct1.Thu}, tiết {Math.Max(ct1.TietBatDau, ct2.TietBatDau)}-{Math.Min(ct1.TietKetThuc, ct2.TietKetThuc)}.");
                    }
                }

                // Kiểm tra vướng lịch bận cố định
                var ct = cacBuoiHocCuaGv[i];
                if (boiCanh.LichBanTheoGv.TryGetValue(maGv, out var dsLb))
                {
                    if (dsLb.Any(lb => lb.Thu == ct.Thu && Math.Max(lb.TietBatDau, ct.TietBatDau) <= Math.Min(lb.TietKetThuc, ct.TietKetThuc)))
                    {
                        ketQua.SoViPhamLichBan++;
                        ketQua.DiemPhatRangBuocCung += boiCanh.TrongSoLichBan;
                        ketQua.DanhSachViPham.Add($"{tenGv} bị vướng lịch bận cố định vào Thứ {ct.Thu}, tiết {ct.TietBatDau}-{ct.TietKetThuc}.");
                    }
                }
            }
        }

        // 2. Kiểm tra RÀNG BUỘC MỀM (Soft Constraints)
        // 2.1 Điểm nguyện vọng
        foreach (var (maGv, dsLopCuaGv) in lopTheoGv)
        {
            foreach (var maLop in dsLopCuaGv)
            {
                ketQua.DiemPhatNguyenVong += TinhDiemNguyenVong(maGv, maLop, boiCanh);
            }
        }

        // 2.2 Điểm cân bằng tải & 2.3 Điểm gom lịch
        foreach (var (maGv, dsLopCuaGv) in lopTheoGv)
        {
            ketQua.DiemPhatCanBangTai += TinhDiemCanBangTai(maGv, dsLopCuaGv, boiCanh);
            ketQua.DiemPhatGomLich += TinhDiemGomLich(maGv, dsLopCuaGv, boiCanh);
        }

        // 3. TỔNG HỢP ĐIỂM PHẠT VÀ ĐỘ THÍCH NGHI (FITNESS)
        ketQua.TongDiemPhat = ketQua.DiemPhatRangBuocCung + ketQua.DiemPhatNguyenVong + ketQua.DiemPhatCanBangTai + ketQua.DiemPhatGomLich;
        ketQua.DoThichNghi = 1.0 / (1.0 + ketQua.TongDiemPhat);

        return ketQua;
    }

    /// <summary>
    /// Tính điểm phạt nguyện vọng: Phạt nếu xếp vào khung giờ tránh dạy hoặc không mong muốn; ưu tiên giảng viên cơ hữu.
    /// </summary>
    public double TinhDiemNguyenVong(int maGiangVien, int maLopHocPhan, BoiCanhDanhGia boiCanh)
    {
        if (maGiangVien <= 0) return 0;

        double diemPhat = 0;
        boiCanh.ChiTietTheoLop.TryGetValue(maLopHocPhan, out var dsChiTiet);
        dsChiTiet ??= [];

        if (boiCanh.NguyenVongTheoGv.TryGetValue(maGiangVien, out var dsNv) && dsNv.Count > 0)
        {
            foreach (var ct in dsChiTiet)
            {
                var nvKhop = dsNv.FirstOrDefault(nv =>
                    nv.Thu == ct.Thu &&
                    Math.Max(nv.TietBatDau ?? 1, ct.TietBatDau) <= Math.Min(nv.TietKetThuc ?? 12, ct.TietKetThuc));

                if (nvKhop != null)
                {
                    if (nvKhop.LoaiNguyenVong == "TranhDay" || nvKhop.MucDo <= 2)
                    {
                        diemPhat += (6 - nvKhop.MucDo) * boiCanh.TrongSoNguyenVong;
                    }
                    else if (nvKhop.MucDo == 3)
                    {
                        diemPhat += 1.0 * boiCanh.TrongSoNguyenVong;
                    }
                    // Mức 4, 5 (Rất ưu tiên) -> 0 điểm phạt
                }
                else
                {
                    // Không có đăng ký ưu tiên cho buổi này -> phạt nhẹ mức trung gian
                    diemPhat += 0.5 * boiCanh.TrongSoNguyenVong;
                }
            }
        }

        // Ưu tiên GV cơ hữu đủ giờ trước khi dùng GV thỉnh giảng
        if (boiCanh.GiangVienTheoMa.TryGetValue(maGiangVien, out var gv))
        {
            bool isThinhGiang = (gv.ChucDanh != null && gv.ChucDanh.Contains("Thỉnh giảng", StringComparison.OrdinalIgnoreCase)) ||
                                gv.HoTen.Contains("Thỉnh giảng", StringComparison.OrdinalIgnoreCase);
            if (isThinhGiang)
            {
                diemPhat += 1.0 * boiCanh.TrongSoCoHuu;
            }
        }

        return diemPhat;
    }

    /// <summary>
    /// Tính điểm phạt cân bằng tải: Phạt nếu số tiết phân công lệch khỏi định mức tối thiểu hoặc tối đa.
    /// </summary>
    public double TinhDiemCanBangTai(int maGiangVien, List<int> dsMaLopPhanCong, BoiCanhDanhGia boiCanh)
    {
        if (maGiangVien <= 0) return 0;

        int tongSoTiet = 0;
        foreach (var maLop in dsMaLopPhanCong)
        {
            if (boiCanh.ChiTietTheoLop.TryGetValue(maLop, out var dsCt))
            {
                foreach (var ct in dsCt)
                {
                    int soTietBuoi = ct.TietKetThuc >= ct.TietBatDau ? (ct.TietKetThuc - ct.TietBatDau + 1) : 0;
                    int soTuan = ct.DenTuan >= ct.TuTuan ? (ct.DenTuan - ct.TuTuan + 1) : 15;
                    tongSoTiet += soTietBuoi * soTuan;
                }
            }
        }

        int minTiet = 60;
        int maxTiet = 120;
        if (boiCanh.DinhMucTheoGv.TryGetValue(maGiangVien, out var dm))
        {
            minTiet = dm.SoTietToiThieu;
            maxTiet = dm.SoTietToiDa;
        }

        double diemPhat = 0;
        if (tongSoTiet < minTiet)
        {
            diemPhat = (minTiet - tongSoTiet) * boiCanh.TrongSoCanBangTai;
        }
        else if (tongSoTiet > maxTiet)
        {
            diemPhat = (tongSoTiet - maxTiet) * boiCanh.TrongSoCanBangTai * 1.5;
        }

        return diemPhat;
    }

    /// <summary>
    /// Tính điểm phạt gom lịch: Phạt các tiết trống giữa ca trong cùng một ngày và phạt nếu đi dạy rải rác nhiều ngày.
    /// </summary>
    public double TinhDiemGomLich(int maGiangVien, List<int> dsMaLopPhanCong, BoiCanhDanhGia boiCanh)
    {
        if (maGiangVien <= 0 || dsMaLopPhanCong.Count == 0) return 0;

        var cacBuoiHoc = new List<ChiTietThoiKhoaBieu>();
        foreach (var maLop in dsMaLopPhanCong)
        {
            if (boiCanh.ChiTietTheoLop.TryGetValue(maLop, out var dsCt))
            {
                cacBuoiHoc.AddRange(dsCt);
            }
        }

        if (cacBuoiHoc.Count == 0) return 0;

        double diemPhat = 0;

        // 1. Phạt tiết trống giữa ca trong cùng một ngày
        var buoiTheoThu = cacBuoiHoc.GroupBy(x => x.Thu);
        foreach (var groupThu in buoiTheoThu)
        {
            var dsSapXep = groupThu.OrderBy(x => x.TietBatDau).ToList();
            for (int i = 0; i < dsSapXep.Count - 1; i++)
            {
                int gap = dsSapXep[i + 1].TietBatDau - dsSapXep[i].TietKetThuc - 1;
                if (gap > 0)
                {
                    diemPhat += gap * (boiCanh.TrongSoGomLich * 0.5);
                }
            }
        }

        // 2. Phạt nếu số ngày dạy quá nhiều so với số lượng lớp (rải rác)
        int soNgayDay = buoiTheoThu.Count();
        int soLop = dsMaLopPhanCong.Count;
        if (soNgayDay > Math.Max(2, soLop))
        {
            diemPhat += (soNgayDay - Math.Max(2, soLop)) * boiCanh.TrongSoGomLich;
        }

        return diemPhat;
    }

    /// <summary>
    /// Nạp và đóng gói toàn bộ dữ liệu của học kỳ từ CSDL thành BoiCanhDanhGia để phục vụ xếp lịch.
    /// </summary>
    public async Task<BoiCanhDanhGia> TaoBoiCanhAsync(int maHocKy, CancellationToken cancellationToken = default)
    {
        if (_context == null) throw new InvalidOperationException("AppDbContext chua duoc khoi tao.");

        var dsLop = await _context.LopHocPhan.Where(x => x.MaHocKy == maHocKy).ToListAsync(cancellationToken);
        var dsMaLop = dsLop.Select(x => x.MaLopHocPhan).ToList();
        var dsChiTiet = await _context.ChiTietThoiKhoaBieu.Where(x => dsMaLop.Contains(x.MaLopHocPhan)).ToListAsync(cancellationToken);
        var dsGiangVien = await _context.GiangVien.Where(x => x.TrangThai != "NghiViec").ToListAsync(cancellationToken);
        var dsNangLuc = await _context.NangLucGiangDay.Where(x => string.IsNullOrEmpty(x.TrangThai) || x.TrangThai == "DangApDung").ToListAsync(cancellationToken);
        var dsLichBan = await _context.LichBanGiangVien.Where(x => x.MaHocKy == maHocKy).ToListAsync(cancellationToken);
        var dsNguyenVong = await _context.NguyenVongGiangVien.Where(x => x.MaHocKy == maHocKy).ToListAsync(cancellationToken);
        var dsDinhMuc = await _context.DinhMucGiangVien.Where(x => x.MaHocKy == maHocKy).ToListAsync(cancellationToken);
        var dsRangBuoc = await _context.RangBuocHocKy.Where(x => x.MaHocKy == maHocKy).ToListAsync(cancellationToken);

        var boiCanh = new BoiCanhDanhGia
        {
            MaHocKy = maHocKy,
            DanhSachLop = dsLop,
            LopTheoMa = dsLop.ToDictionary(x => x.MaLopHocPhan),
            ChiTietTheoLop = dsChiTiet.GroupBy(x => x.MaLopHocPhan).ToDictionary(g => g.Key, g => g.ToList()),
            GiangVienTheoMa = dsGiangVien.ToDictionary(x => x.MaGiangVien),
            NangLucTheoGv = dsNangLuc.GroupBy(x => x.MaGiangVien).ToDictionary(g => g.Key, g => g.Select(x => x.MaHocPhan).ToHashSet()),
            LichBanTheoGv = dsLichBan.GroupBy(x => x.MaGiangVien).ToDictionary(g => g.Key, g => g.ToList()),
            NguyenVongTheoGv = dsNguyenVong.GroupBy(x => x.MaGiangVien).ToDictionary(g => g.Key, g => g.ToList()),
            DinhMucTheoGv = dsDinhMuc.ToDictionary(x => x.MaGiangVien),
            TrongSoRangBuoc = dsRangBuoc.GroupBy(x => x.TenRangBuoc).ToDictionary(g => g.Key, g => g.First().TrongSoPhat)
        };

        return boiCanh;
    }

    // ==========================================
    // CÁC HÀM CŨ ĐỂ ĐẢM BẢO TƯƠNG THÍCH MÃ NGUỒN
    // ==========================================
    public double TinhDiemNguyenVong(int maGiangVien, int maLopHocPhan)
    {
        _ = (maGiangVien, maLopHocPhan);
        return 0;
    }

    public double TinhDiemGomLich(int maGiangVien)
    {
        _ = maGiangVien;
        return 0;
    }

    public double TinhDiemCanBangTai(int maGiangVien)
    {
        _ = maGiangVien;
        return 0;
    }

    public double TinhTongDiemPhat(IEnumerable<double> danhSachDiem)
    {
        return danhSachDiem.Sum();
    }
}
