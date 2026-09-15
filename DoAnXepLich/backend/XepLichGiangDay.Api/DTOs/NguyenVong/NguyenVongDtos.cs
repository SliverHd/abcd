namespace XepLichGiangDay.Api.DTOs.NguyenVong;

public class LichBanDto
{
    public int MaLichBan { get; set; }
    public int MaGiangVien { get; set; }
    public int MaHocKy { get; set; }
    public int Thu { get; set; }
    public int TietBatDau { get; set; }
    public int TietKetThuc { get; set; }
    public string? LyDo { get; set; }
}

public class TaoLichBanDto
{
    public int Thu { get; set; }
    public int TietBatDau { get; set; }
    public int TietKetThuc { get; set; }
    public string? LyDo { get; set; }
}

public class NguyenVongDto
{
    public int MaNguyenVong { get; set; }
    public int MaGiangVien { get; set; }
    public string? TenGiangVien { get; set; }
    public int MaHocKy { get; set; }
    public string MaRangBuocMem { get; set; } = "SC01";
    public string TenRangBuocMem { get; set; } = string.Empty;
    public string LoaiNguyenVong { get; set; } = "TranhDay"; // ThichDay, TranhDay
    public int MucDo { get; set; } = 1;
    public int? Thu { get; set; }
    public int? TietBatDau { get; set; }
    public int? TietKetThuc { get; set; }
    public double TrongSoPhat { get; set; }
    public string? NoiDungGoc { get; set; }
    public string? LyDo { get; set; }
}

public class ThemNguyenVongDto
{
    public int MaHocKy { get; set; }
    public int MaGiangVien { get; set; }
    public string MaRangBuocMem { get; set; } = "SC01";
    public string? TenRangBuocMem { get; set; }
    public int? Thu { get; set; }
    public int? TietBatDau { get; set; }
    public int? TietKetThuc { get; set; }
    public double TrongSoPhat { get; set; } = 10.0;
    public string? LyDo { get; set; }
}

public class TaoNguyenVongDto
{
    public string LoaiNguyenVong { get; set; } = "ThichDay";
    public int MucDo { get; set; } = 1;
    public int? Thu { get; set; }
    public int? TietBatDau { get; set; }
    public int? TietKetThuc { get; set; }
    public string? GhiChu { get; set; }
}

public class LuuMaTranNguyenVongRequest
{
    public int MaGiangVien { get; set; }
    public int MaHocKy { get; set; }
    public List<TaoLichBanDto> DanhSachLichBan { get; set; } = [];
    public List<TaoNguyenVongDto> DanhSachNguyenVong { get; set; } = [];
}

