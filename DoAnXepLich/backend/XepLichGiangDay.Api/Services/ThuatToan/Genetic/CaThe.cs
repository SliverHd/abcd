using XepLichGiangDay.Api.Services.RangBuoc;

namespace XepLichGiangDay.Api.Services.ThuatToan.Genetic;

public class CaThe
{
    public List<Gen> DanhSachGen { get; set; } = [];
    public double DiemPhat { get; set; }
    public KetQuaDanhGiaPhuongAn? KetQuaDanhGia { get; set; }
}
