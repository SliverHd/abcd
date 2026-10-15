namespace XepLichGiangDay.Api.DTOs.GiangVien;

public class TaoGiangVienDto
{
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string? ChucDanh { get; set; }
    public int? MaBoMon { get; set; }
    public string TrangThai { get; set; } = "DangLamViec";
    public List<int>? DanhSachMaHocPhan { get; set; }
}
