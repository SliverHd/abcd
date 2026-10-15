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

        return await query.Select(x => new NguyenVongDto
        {
            MaNguyenVong = x.MaNguyenVong,
            MaGiangVien = x.MaGiangVien,
            MaHocKy = x.MaHocKy,
            LoaiNguyenVong = x.LoaiNguyenVong,
            MucDo = x.MucDo,
            Thu = x.Thu,
            TietBatDau = x.TietBatDau,
            TietKetThuc = x.TietKetThuc,
            TrongSoPhat = x.TrongSoPhat,
            NoiDungGoc = x.NoiDungGoc
        }).ToListAsync();
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
