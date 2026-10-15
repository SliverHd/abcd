namespace XepLichGiangDay.Api.DTOs.GiangVien;

public class GiangVienDto
{
    public int MaGiangVien { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string? ChucDanh { get; set; }
    public int? MaBoMon { get; set; }
    public string? TenBoMon { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public List<NangLucGiangDayDto> DanhSachNangLuc { get; set; } = [];
}
