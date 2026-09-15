using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.NguyenVong;
using XepLichGiangDay.Api.Models.Entities;

namespace XepLichGiangDay.Api.Services;

public class NguyenVongService(AppDbContext context)
{
    public async Task<List<LichBanDto>> LayLichBanAsync(int maHocKy, int? maGiangVien = null)
    {
        var query = context.LichBanGiangVien.Where(x => x.MaHocKy == maHocKy);
        if (maGiangVien.HasValue && maGiangVien.Value > 0)
        {
            query = query.Where(x => x.MaGiangVien == maGiangVien.Value);
        }

        return await query.Select(x => new LichBanDto
        {
            MaLichBan = x.MaLichBan,
            MaGiangVien = x.MaGiangVien,
            MaHocKy = x.MaHocKy,
            Thu = x.Thu,
            TietBatDau = x.TietBatDau,
            TietKetThuc = x.TietKetThuc,
            LyDo = x.LyDo
        }).ToListAsync();
    }

    public async Task<List<NguyenVongDto>> LayNguyenVongAsync(int maHocKy, int? maGiangVien = null)
    {
        var query = context.NguyenVongGiangVien.Where(x => x.MaHocKy == maHocKy);
        if (maGiangVien.HasValue && maGiangVien.Value > 0)
        {
            query = query.Where(x => x.MaGiangVien == maGiangVien.Value);
        }

        var danhSach = await query.ToListAsync();
        var dsGvId = danhSach.Select(x => x.MaGiangVien).Distinct().ToList();
        var dsGiangVien = await context.GiangVien
            .Where(x => dsGvId.Contains(x.MaGiangVien))
            .ToDictionaryAsync(x => x.MaGiangVien);

        return danhSach.Select(x =>
        {
            dsGiangVien.TryGetValue(x.MaGiangVien, out var gv);
            var (maRb, tenRb) = PhanTichMaRangBuoc(x.LoaiNguyenVong, x.NoiDungGoc);
            return new NguyenVongDto
            {
                MaNguyenVong = x.MaNguyenVong,
                MaGiangVien = x.MaGiangVien,
                TenGiangVien = gv?.HoTen ?? $"GV #{x.MaGiangVien}",
                MaHocKy = x.MaHocKy,
                MaRangBuocMem = maRb,
                TenRangBuocMem = tenRb,
                LoaiNguyenVong = x.LoaiNguyenVong,
                MucDo = x.MucDo,
                Thu = x.Thu,
                TietBatDau = x.TietBatDau,
                TietKetThuc = x.TietKetThuc,
                TrongSoPhat = x.TrongSoPhat,
                NoiDungGoc = x.NoiDungGoc,
                LyDo = x.NoiDungGoc
            };
        }).OrderBy(x => x.TenGiangVien).ThenBy(x => x.Thu).ThenBy(x => x.TietBatDau).ToList();
    }

    public async Task<NguyenVongDto?> ThemNguyenVongAsync(ThemNguyenVongDto dto)
    {
        if (dto.MaHocKy <= 0 || dto.MaGiangVien <= 0) return null;

        var entity = new NguyenVongGiangVien
        {
            MaGiangVien = dto.MaGiangVien,
            MaHocKy = dto.MaHocKy,
            LoaiNguyenVong = string.IsNullOrWhiteSpace(dto.MaRangBuocMem) ? "SC01" : dto.MaRangBuocMem.Trim(),
            MucDo = 1,
            Thu = dto.Thu,
            TietBatDau = dto.TietBatDau,
            TietKetThuc = dto.TietKetThuc,
            TrongSoPhat = dto.TrongSoPhat > 0 ? dto.TrongSoPhat : 10.0,
            NoiDungGoc = dto.LyDo?.Trim()
        };

        context.NguyenVongGiangVien.Add(entity);
        await context.SaveChangesAsync();

        var gv = await context.GiangVien.FindAsync(dto.MaGiangVien);
        var (maRb, tenRb) = PhanTichMaRangBuoc(entity.LoaiNguyenVong, entity.NoiDungGoc);

        return new NguyenVongDto
        {
            MaNguyenVong = entity.MaNguyenVong,
            MaGiangVien = entity.MaGiangVien,
            TenGiangVien = gv?.HoTen ?? $"GV #{entity.MaGiangVien}",
            MaHocKy = entity.MaHocKy,
            MaRangBuocMem = maRb,
            TenRangBuocMem = tenRb,
            LoaiNguyenVong = entity.LoaiNguyenVong,
            MucDo = entity.MucDo,
            Thu = entity.Thu,
            TietBatDau = entity.TietBatDau,
            TietKetThuc = entity.TietKetThuc,
            TrongSoPhat = entity.TrongSoPhat,
            NoiDungGoc = entity.NoiDungGoc,
            LyDo = entity.NoiDungGoc
        };
    }

    public async Task<bool> XoaNguyenVongAsync(int maNguyenVong)
    {
        var item = await context.NguyenVongGiangVien.FindAsync(maNguyenVong);
        if (item is null) return false;

        context.NguyenVongGiangVien.Remove(item);
        await context.SaveChangesAsync();
        return true;
    }

    public static (string MaRb, string TenRb) PhanTichMaRangBuoc(string? loai, string? noiDung)
    {
        if (string.IsNullOrWhiteSpace(loai)) return ("SC01", "Tránh khung giờ / tiết bận");

        return loai.Trim() switch
        {
            "SC01" or "TranhDay" => ("SC01", "Tránh khung giờ / tiết bận"),
            "SC02" => ("SC02", "Tránh dạy ca tối (Tiết 10 - 12)"),
            "SC03" => ("SC03", "Tránh dạy ngày Thứ Bảy / Chủ Nhật"),
            "SC04" or "ThichDay" => ("SC04", "Ưu tiên dạy buổi sáng (Tiết 1 - 6)"),
            "SC05" => ("SC05", "Ưu tiên dạy buổi chiều (Tiết 7 - 12)"),
            "SC06" => ("SC06", "Giới hạn số tiết tối đa / ngày"),
            "SC07" => ("SC07", "Hạn chế số ngày dạy trong tuần (gom lịch)"),
            _ => (loai, "Nguyện vọng giảng viên")
        };
    }


    public async Task<bool> LuuMaTranAsync(LuuMaTranNguyenVongRequest request)
    {
        // 1. Cập nhật Lịch bận cố định (BUSY)
        var oldLichBan = await context.LichBanGiangVien
            .Where(x => x.MaGiangVien == request.MaGiangVien && x.MaHocKy == request.MaHocKy)
            .ToListAsync();
        context.LichBanGiangVien.RemoveRange(oldLichBan);

        foreach (var lb in request.DanhSachLichBan)
        {
            context.LichBanGiangVien.Add(new LichBanGiangVien
            {
                MaGiangVien = request.MaGiangVien,
                MaHocKy = request.MaHocKy,
                Thu = lb.Thu,
                TietBatDau = lb.TietBatDau,
                TietKetThuc = lb.TietKetThuc,
                LyDo = lb.LyDo ?? "Bận"
            });
        }

        // 2. Cập nhật Nguyện vọng giảng dạy (PREFERRED / AVOID)
        var oldNguyenVong = await context.NguyenVongGiangVien
            .Where(x => x.MaGiangVien == request.MaGiangVien && x.MaHocKy == request.MaHocKy)
            .ToListAsync();
        context.NguyenVongGiangVien.RemoveRange(oldNguyenVong);

        foreach (var nv in request.DanhSachNguyenVong)
        {
            context.NguyenVongGiangVien.Add(new NguyenVongGiangVien
            {
                MaGiangVien = request.MaGiangVien,
                MaHocKy = request.MaHocKy,
                LoaiNguyenVong = nv.LoaiNguyenVong,
                MucDo = nv.MucDo > 0 ? nv.MucDo : 1,
                Thu = nv.Thu,
                TietBatDau = nv.TietBatDau,
                TietKetThuc = nv.TietKetThuc,
                TrongSoPhat = nv.LoaiNguyenVong == "TranhDay" ? 10.0 : 0.0,
                NoiDungGoc = nv.GhiChu
            });
        }

        await context.SaveChangesAsync();
        return true;
    }
}
