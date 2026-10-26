using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.HocPhan;
using XepLichGiangDay.Api.Models.Entities;

namespace XepLichGiangDay.Api.Services;

public class HocPhanService(AppDbContext context)
{
    public async Task<List<HocPhanDto>> GetAllAsync()
    {
        return await context.HocPhan
            .Select(x => new HocPhanDto
            {
                MaHocPhan = x.MaHocPhan,
                TenHocPhan = x.TenHocPhan,
                SoTinChi = x.SoTinChi,
                TrangThai = x.TrangThai
            })
            .ToListAsync();
    }

    public async Task<HocPhanDto?> GetByIdAsync(int id)
    {
        var item = await context.HocPhan.FindAsync(id);
        if (item is null) return null;

        return new HocPhanDto
        {
            MaHocPhan = item.MaHocPhan,
            TenHocPhan = item.TenHocPhan,
            SoTinChi = item.SoTinChi,
            TrangThai = item.TrangThai
        };
    }

    public async Task<HocPhanDto> CreateAsync(TaoHocPhanDto dto)
    {
        var item = new HocPhan
        {
            TenHocPhan = dto.TenHocPhan,
            SoTinChi = dto.SoTinChi,
            TrangThai = dto.TrangThai
        };

        context.HocPhan.Add(item);
        await context.SaveChangesAsync();

        return new HocPhanDto
        {
            MaHocPhan = item.MaHocPhan,
            TenHocPhan = item.TenHocPhan,
            SoTinChi = item.SoTinChi,
            TrangThai = item.TrangThai
        };
    }

    public async Task<bool> UpdateAsync(int id, TaoHocPhanDto dto)
    {
        var item = await context.HocPhan.FindAsync(id);
        if (item is null) return false;

        item.TenHocPhan = dto.TenHocPhan;
        item.SoTinChi = dto.SoTinChi;
        item.TrangThai = dto.TrangThai;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var item = await context.HocPhan.FindAsync(id);
        if (item is null) return (false, "Không tìm thấy học phần.");

        var coLop = await context.LopHocPhan.AnyAsync(x => x.MaHocPhan == id);
        if (coLop)
        {
            return (false, "Không thể xóa học phần đã có lớp học phần mở trong hệ thống.");
        }

        context.HocPhan.Remove(item);
        await context.SaveChangesAsync();
        return (true, null);
    }

}
