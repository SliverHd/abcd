namespace XepLichGiangDay.Api.DTOs.XepLich;

public class DuLieuXepLichDto
{
    public int MaHocKy { get; set; }
    public string ThuatToan { get; set; } = string.Empty;
    public int? KichThuocQuanThe { get; set; }
    public int? SoTheHe { get; set; }
    public double? TyLeLaiGhep { get; set; }
    public double? TyLeDotBien { get; set; }

    public int? RandomSeed { get; set; }
    public int? KichThuocGiaiDau { get; set; }
    public int? SoCaTheUuTu { get; set; }

    // Tham số cấu hình tối ưu cho CP-SAT (Google OR-Tools)
    public double? ThoiGianChayToiDa { get; set; } = 30.0;
    public int? SoLuongWorker { get; set; } = 1; // 1 luồng đảm bảo tính tất định 100%
    public double? KhoangCachToiUuGap { get; set; } = 0.01;
}

