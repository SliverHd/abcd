namespace XepLichGiangDay.Api.Services.ThuatToan.Genetic;

public class CauHinhGa
{
    public int KichThuocQuanThe { get; set; } = 50;
    public int SoTheHe { get; set; } = 100;
    public double TyLeLaiGhep { get; set; } = 0.8;
    public double TyLeDotBien { get; set; } = 0.05;
    public int KichThuocGiaiDau { get; set; } = 3;
    public int SoCaTheUuTu { get; set; } = 2;
    public int? RandomSeed { get; set; }
    public double ThoiGianChayToiDaGiay { get; set; } = 30.0;

    /// <summary>
    /// Chuẩn hóa và xác thực tính hợp lệ của tham số cấu hình.
    /// </summary>
    public void ChuanHoaVaXacThuc()
    {
        if (KichThuocQuanThe < 4) KichThuocQuanThe = 4;
        if (SoTheHe < 1) SoTheHe = 1;
        if (TyLeLaiGhep < 0.0) TyLeLaiGhep = 0.0;
        if (TyLeLaiGhep > 1.0) TyLeLaiGhep = 1.0;
        if (TyLeDotBien < 0.0) TyLeDotBien = 0.0;
        if (TyLeDotBien > 1.0) TyLeDotBien = 1.0;
        if (KichThuocGiaiDau < 2) KichThuocGiaiDau = 2;
        if (KichThuocGiaiDau > KichThuocQuanThe) KichThuocGiaiDau = KichThuocQuanThe;
        if (SoCaTheUuTu < 1) SoCaTheUuTu = 1;
        if (SoCaTheUuTu >= KichThuocQuanThe) SoCaTheUuTu = KichThuocQuanThe / 2;
        if (ThoiGianChayToiDaGiay <= 0) ThoiGianChayToiDaGiay = 30.0;
    }
}

