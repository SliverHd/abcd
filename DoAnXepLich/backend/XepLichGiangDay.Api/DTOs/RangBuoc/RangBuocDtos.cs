namespace XepLichGiangDay.Api.DTOs.RangBuoc;

public class RangBuocHocKyDto
{
    public int MaRangBuoc { get; set; }
    public int MaHocKy { get; set; }
    public string TenRangBuoc { get; set; } = string.Empty;
    public string LoaiRangBuoc { get; set; } = string.Empty;
    public double TrongSoPhat { get; set; }
    public string? MoTa { get; set; }
}

public class CapNhatRangBuocDto
{
    public double TrongSoPhat { get; set; }
}

public class DinhMucGiangVienDto
{
    public int MaDinhMuc { get; set; }
    public int MaGiangVien { get; set; }
    public string TenGiangVien { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int MaHocKy { get; set; }
    public int SoTietToiThieu { get; set; }
    public int SoTietToiDa { get; set; }
}

public class CapNhatDinhMucDto
{
    public int MaGiangVien { get; set; }
    public int MaHocKy { get; set; }
    public int SoTietToiThieu { get; set; }
    public int SoTietToiDa { get; set; }
}

public class ApDungDinhMucChungDto
{
    public int MaHocKy { get; set; }
    public int SoTietToiThieu { get; set; }
    public int SoTietToiDa { get; set; }
}

public class UngVienLopHocPhanDto
{
    public int MaLopHocPhan { get; set; }
    public string MaLopHocPhanTruong { get; set; } = string.Empty;
    public string TenLop { get; set; } = string.Empty;
    public int MaHocPhan { get; set; }
    public string TenHocPhan { get; set; } = string.Empty;
    public List<ThongTinUngVienDto> DanhSachUngVien { get; set; } = [];
}

public class ThongTinUngVienDto
{
    public int MaGiangVien { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ChucDanh { get; set; }
    public bool CoNangLuc { get; set; }
    public bool BiVuongLichBan { get; set; }
    public bool HopLe { get; set; }
}

public class KetQuaDanhGiaDto
{
    public double TongDiemPhat { get; set; }
    public double DoThichNghi { get; set; }
    public double DiemPhatRangBuocCung { get; set; }
    public double DiemPhatNguyenVong { get; set; }
    public double DiemPhatCanBangTai { get; set; }
    public double DiemPhatGomLich { get; set; }
    public int SoViPhamCung { get; set; }
    public bool HopLe { get; set; }
    public List<string> DanhSachViPham { get; set; } = [];
}

