namespace XepLichGiangDay.Api.DTOs.GiangVien;

public class NangLucGiangDayDto
{
    public int MaNangLuc { get; set; }
    public int MaGiangVien { get; set; }
    public int MaHocPhan { get; set; }
    public string TenHocPhan { get; set; } = string.Empty;
    public int SoTinChi { get; set; } = 3;
    public int MucDoChuyenMon { get; set; } = 1;
    public string TrangThai { get; set; } = "DangApDung";
}

public class CapNhatNangLucDto
{
    public List<int> DanhSachMaHocPhan { get; set; } = [];
}
