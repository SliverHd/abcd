using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.RangBuoc;
using XepLichGiangDay.Api.Models.Entities;

namespace XepLichGiangDay.Api.Services.RangBuoc;

public class TaoDanhSachUngVien
{
    private readonly AppDbContext? _context;

    public TaoDanhSachUngVien()
    {
    }

    public TaoDanhSachUngVien(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Tạo danh sách giảng viên ứng viên cho từng lớp học phần (xử lý in-memory siêu tốc, tối ưu cho thuật toán GA/CP-SAT).
    /// </summary>
    public Dictionary<int, List<int>> TaoToanBo(
        List<LopHocPhan> dsLop,
        List<ChiTietThoiKhoaBieu> dsChiTiet,
        List<NangLucGiangDay> dsNangLuc,
        List<LichBanGiangVien> dsLichBan,
        List<GiangVien> dsGiangVien,
        bool boQuaLichBanNeuKhongCoUngVien = true)
    {
        var ketQua = new Dictionary<int, List<int>>();

        // 1. Nhóm chi tiết thời khóa biểu theo lớp học phần
        var chiTietTheoLop = dsChiTiet
            .GroupBy(x => x.MaLopHocPhan)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 2. Lọc danh sách giảng viên đang làm việc (không tính người đã nghỉ việc)
        var dsGvHoatDong = dsGiangVien
            .Where(g => g.TrangThai != "NghiViec")
            .Select(g => g.MaGiangVien)
            .ToHashSet();

        // 3. Nhóm năng lực giảng dạy theo mã học phần
        var gvTheoHocPhan = dsNangLuc
            .Where(nl => dsGvHoatDong.Contains(nl.MaGiangVien) && (string.IsNullOrEmpty(nl.TrangThai) || nl.TrangThai == "DangApDung"))
            .GroupBy(nl => nl.MaHocPhan)
            .ToDictionary(g => g.Key, g => g.Select(x => x.MaGiangVien).Distinct().ToList());

        // 4. Nhóm lịch bận cố định theo giảng viên
        var lichBanTheoGv = dsLichBan
            .GroupBy(lb => lb.MaGiangVien)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 5. Duyệt từng lớp để lọc ứng viên
        foreach (var lop in dsLop)
        {
            if (!gvTheoHocPhan.TryGetValue(lop.MaHocPhan, out var dsGvChuyenMon) || dsGvChuyenMon.Count == 0)
            {
                ketQua[lop.MaLopHocPhan] = [];
                continue;
            }

            chiTietTheoLop.TryGetValue(lop.MaLopHocPhan, out var cacBuoiHoc);
            cacBuoiHoc ??= [];

            var ungVienHopLe = new List<int>();

            foreach (var maGv in dsGvChuyenMon)
            {
                bool biVuongLichBan = false;
                if (lichBanTheoGv.TryGetValue(maGv, out var dsLbCuaGv))
                {
                    foreach (var bh in cacBuoiHoc)
                    {
                        if (dsLbCuaGv.Any(lb => lb.Thu == bh.Thu && Math.Max(lb.TietBatDau, bh.TietBatDau) <= Math.Min(lb.TietKetThuc, bh.TietKetThuc)))
                        {
                            biVuongLichBan = true;
                            break;
                        }
                    }
                }

                if (!biVuongLichBan)
                {
                    ungVienHopLe.Add(maGv);
                }
            }

            // Nếu sau khi lọc lịch bận mà không còn ai, và cho phép fallback sang người có năng lực để tránh bế tắc thuật toán
            if (ungVienHopLe.Count == 0 && boQuaLichBanNeuKhongCoUngVien)
            {
                ungVienHopLe = [.. dsGvChuyenMon];
            }

            ketQua[lop.MaLopHocPhan] = ungVienHopLe;
        }

        return ketQua;
    }

    /// <summary>
    /// Lấy danh sách ID ứng viên cho tất cả các lớp của học kỳ từ cơ sở dữ liệu.
    /// </summary>
    public async Task<Dictionary<int, List<int>>> TaoToanBoTheoHocKyAsync(int maHocKy, CancellationToken cancellationToken = default)
    {
        if (_context == null) throw new InvalidOperationException("AppDbContext chua duoc khoi tao.");

        var dsLop = await _context.LopHocPhan.Where(x => x.MaHocKy == maHocKy).ToListAsync(cancellationToken);
        var dsMaLop = dsLop.Select(x => x.MaLopHocPhan).ToList();
        var dsChiTiet = await _context.ChiTietThoiKhoaBieu.Where(x => dsMaLop.Contains(x.MaLopHocPhan)).ToListAsync(cancellationToken);
        var dsNangLuc = await _context.NangLucGiangDay.ToListAsync(cancellationToken);
        var dsLichBan = await _context.LichBanGiangVien.Where(x => x.MaHocKy == maHocKy).ToListAsync(cancellationToken);
        var dsGiangVien = await _context.GiangVien.ToListAsync(cancellationToken);

        return TaoToanBo(dsLop, dsChiTiet, dsNangLuc, dsLichBan, dsGiangVien);
    }

    /// <summary>
    /// Lấy danh sách ứng viên chi tiết (đầy đủ tên, chuyên môn, trạng thái bận) phục vụ hiển thị UI và chỉnh sửa thủ công.
    /// </summary>
    public async Task<List<UngVienLopHocPhanDto>> LayDanhSachUngVienChiTietAsync(int maHocKy, CancellationToken cancellationToken = default)
    {
        if (_context == null) throw new InvalidOperationException("AppDbContext chua duoc khoi tao.");

        var dsLop = await _context.LopHocPhan
            .Where(x => x.MaHocKy == maHocKy)
            .ToListAsync(cancellationToken);

        var dsMaHp = dsLop.Select(x => x.MaHocPhan).Distinct().ToList();
        var dsHocPhan = await _context.HocPhan.Where(x => dsMaHp.Contains(x.MaHocPhan)).ToListAsync(cancellationToken);
        var dictHp = dsHocPhan.ToDictionary(x => x.MaHocPhan);

        var dsMaLop = dsLop.Select(x => x.MaLopHocPhan).ToList();
        var dsChiTiet = await _context.ChiTietThoiKhoaBieu.Where(x => dsMaLop.Contains(x.MaLopHocPhan)).ToListAsync(cancellationToken);
        var chiTietTheoLop = dsChiTiet.GroupBy(x => x.MaLopHocPhan).ToDictionary(g => g.Key, g => g.ToList());

        var dsGiangVien = await _context.GiangVien.Where(g => g.TrangThai != "NghiViec").ToListAsync(cancellationToken);
        var dsNangLuc = await _context.NangLucGiangDay.ToListAsync(cancellationToken);
        var dsLichBan = await _context.LichBanGiangVien.Where(x => x.MaHocKy == maHocKy).ToListAsync(cancellationToken);
        var lichBanTheoGv = dsLichBan.GroupBy(x => x.MaGiangVien).ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<UngVienLopHocPhanDto>();

        foreach (var lop in dsLop)
        {
            var tenHp = dictHp.TryGetValue(lop.MaHocPhan, out var hp) ? hp.TenHocPhan : "";
            chiTietTheoLop.TryGetValue(lop.MaLopHocPhan, out var cacBuoiHoc);
            cacBuoiHoc ??= [];

            var ungVienDto = new UngVienLopHocPhanDto
            {
                MaLopHocPhan = lop.MaLopHocPhan,
                MaLopHocPhanTruong = lop.MaLopHocPhanTruong,
                TenLop = lop.TenLop,
                MaHocPhan = lop.MaHocPhan,
                TenHocPhan = tenHp
            };

            // Lấy danh sách GV có năng lực môn này
            var dsGvIdNangLuc = dsNangLuc
                .Where(nl => nl.MaHocPhan == lop.MaHocPhan && (string.IsNullOrEmpty(nl.TrangThai) || nl.TrangThai == "DangApDung"))
                .Select(nl => nl.MaGiangVien)
                .ToHashSet();

            foreach (var gv in dsGiangVien)
            {
                bool coNangLuc = dsGvIdNangLuc.Contains(gv.MaGiangVien);
                if (!coNangLuc) continue; // Chỉ liệt kê những người có chuyên môn

                bool biVuongLichBan = false;
                if (lichBanTheoGv.TryGetValue(gv.MaGiangVien, out var dsLb))
                {
                    foreach (var bh in cacBuoiHoc)
                    {
                        if (dsLb.Any(lb => lb.Thu == bh.Thu && Math.Max(lb.TietBatDau, bh.TietBatDau) <= Math.Min(lb.TietKetThuc, bh.TietKetThuc)))
                        {
                            biVuongLichBan = true;
                            break;
                        }
                    }
                }

                ungVienDto.DanhSachUngVien.Add(new ThongTinUngVienDto
                {
                    MaGiangVien = gv.MaGiangVien,
                    HoTen = gv.HoTen,
                    Email = gv.Email,
                    ChucDanh = gv.ChucDanh,
                    CoNangLuc = true,
                    BiVuongLichBan = biVuongLichBan,
                    HopLe = !biVuongLichBan
                });
            }

            // Sắp xếp: người không vướng lịch bận lên trước
            ungVienDto.DanhSachUngVien = ungVienDto.DanhSachUngVien
                .OrderByDescending(x => x.HopLe)
                .ThenBy(x => x.HoTen)
                .ToList();

            result.Add(ungVienDto);
        }

        return result;
    }

    /// <summary>
    /// Lấy danh sách ID ứng viên cho một lớp học phần cụ thể.
    /// </summary>
    public async Task<List<int>> TaoTheoLopHocPhanAsync(int maLopHocPhan, CancellationToken cancellationToken = default)
    {
        if (_context == null) throw new InvalidOperationException("AppDbContext chua duoc khoi tao.");

        var lop = await _context.LopHocPhan.FirstOrDefaultAsync(x => x.MaLopHocPhan == maLopHocPhan, cancellationToken);
        if (lop == null) return [];

        var dsChiTiet = await _context.ChiTietThoiKhoaBieu.Where(x => x.MaLopHocPhan == maLopHocPhan).ToListAsync(cancellationToken);
        var dsNangLuc = await _context.NangLucGiangDay.Where(x => x.MaHocPhan == lop.MaHocPhan).ToListAsync(cancellationToken);
        var dsLichBan = await _context.LichBanGiangVien.Where(x => x.MaHocKy == lop.MaHocKy).ToListAsync(cancellationToken);
        var dsGiangVien = await _context.GiangVien.ToListAsync(cancellationToken);

        var ketQua = TaoToanBo([lop], dsChiTiet, dsNangLuc, dsLichBan, dsGiangVien);
        return ketQua.TryGetValue(maLopHocPhan, out var list) ? list : [];
    }

    public List<int> TaoTheoLopHocPhan(int maLopHocPhan)
    {
        if (_context == null) return [];
        return TaoTheoLopHocPhanAsync(maLopHocPhan).GetAwaiter().GetResult();
    }
}
