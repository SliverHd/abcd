using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;

namespace XepLichGiangDay.Api.Services;

public class ThoiKhoaBieuService(AppDbContext context)
{
    public async Task<List<object>> LayDanhSachLopHocPhanAsync(int? maHocKy)
    {
        var query = context.LopHocPhan.AsQueryable();
        if (maHocKy.HasValue && maHocKy.Value > 0)
        {
            query = query.Where(x => x.MaHocKy == maHocKy.Value);
        }

        return await (from lop in query
                      join hp in context.HocPhan on lop.MaHocPhan equals hp.MaHocPhan into hpGroup
                      from hp in hpGroup.DefaultIfEmpty()
                      join ct in context.ChiTietThoiKhoaBieu on lop.MaLopHocPhan equals ct.MaLopHocPhan into ctGroup
                      from ct in ctGroup.DefaultIfEmpty()
                      select new
                      {
                          lop.MaLopHocPhan,
                          lop.MaLopHocPhanTruong,
                          lop.TenLop,
                          lop.MaHocKy,
                          TenHocPhan = hp != null ? hp.TenHocPhan : "",
                          SoTinChi = hp != null ? hp.SoTinChi : 0,
                          lop.SoLuongSinhVien,
                          Thu = ct != null ? ct.Thu : 0,
                          TietBatDau = ct != null ? ct.TietBatDau : 0,
                          TietKetThuc = ct != null ? ct.TietKetThuc : 0,
                          PhongHoc = ct != null ? ct.PhongHoc : "",
                          TuTuan = ct != null ? ct.TuTuan : 0,
                          DenTuan = ct != null ? ct.DenTuan : 0
                      }).ToListAsync<object>();
    }

    public async Task<(bool ThanhCong, string? ThongBaoLoi)> XoaLopHocPhanAsync(int id)
    {
        var item = await context.LopHocPhan.FindAsync(id);
        if (item == null) return (false, "Lop hoc phan khong ton tai.");

        var daPhanCong = await context.PhanCongGiangDay.AnyAsync(x => x.MaLopHocPhan == id);
        if (daPhanCong)
        {
            return (false, "Không thể xóa lớp học phần đã được phân công giảng dạy.");
        }

        // Dọn dẹp bản ghi nhật ký xếp lịch liên kết để tránh lỗi khóa ngoại Restrict
        var dsKq = await context.KetQuaXepLich.Where(x => x.MaLopHocPhan == id).ToListAsync();
        if (dsKq.Count > 0)
        {
            context.KetQuaXepLich.RemoveRange(dsKq);
        }

        context.LopHocPhan.Remove(item);
        await context.SaveChangesAsync();
        return (true, null);
    }

}
