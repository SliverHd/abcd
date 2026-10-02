namespace XepLichGiangDay.Api.DTOs.XepLich;

public class KetQuaXepLichDto
{
    public string ThuatToan { get; set; } = string.Empty;
    public double TongDiemPhat { get; set; }
    public double DoThichNghi { get; set; }
    public double DiemPhatRangBuocCung { get; set; }
    public double DiemPhatNguyenVong { get; set; }
    public double DiemPhatCanBangTai { get; set; }
    public double DiemPhatGomLich { get; set; }
    public int SoViPhamCung { get; set; }
    public int SoLopChuaPhanCong { get; set; }
    public int SoTheHeThucTe { get; set; }
    public long ThoiGianChayMiligiay { get; set; }
    public string TrangThai { get; set; } = "ThanhCong";
    public List<double> LichSuDiemPhatTheoTheHe { get; set; } = [];
    public List<string> DanhSachViPham { get; set; } = [];
    public List<PhanCongDto> DanhSachPhanCong { get; set; } = [];

    // Chỉ số hiệu năng và thống kê thuật toán CP-SAT
    public double ThoiGianChayGiay { get; set; }
    public int SoLuongBien { get; set; }
    public int SoLuongRangBuoc { get; set; }
    public string ThongDiep { get; set; } = string.Empty;
    public List<string> ChiTietViPham { get; set; } = [];
}
