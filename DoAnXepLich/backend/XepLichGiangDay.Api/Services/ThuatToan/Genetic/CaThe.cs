using XepLichGiangDay.Api.Services.RangBuoc;

namespace XepLichGiangDay.Api.Services.ThuatToan.Genetic;

public class CaThe
{
    public List<Gen> DanhSachGen { get; set; } = [];
    public double DiemPhat { get; set; }
    public int SoViPhamCung { get; set; }
    public double TongDiemPhatMem { get; set; }
    public double DoThichNghi { get; set; }
    public KetQuaDanhGiaPhuongAn? KetQuaDanhGia { get; set; }

    public bool LaHopLe => SoViPhamCung == 0;

    /// <summary>
    /// So sánh 2 cá thể theo nguyên lý phân tầng (Lexicographic / Barrier):
    /// - Cá thể có ít vi phạm cứng hơn LUÔN tốt hơn (trả về số âm).
    /// - Khi số vi phạm cứng bằng nhau: cá thể có tổng điểm phạt mềm thấp hơn sẽ tốt hơn.
    /// </summary>
    public int SoSanhVoi(CaThe other)
    {
        if (other == null) return -1;

        // 1. Ưu tiên tuyệt đối ràng buộc cứng
        if (SoViPhamCung != other.SoViPhamCung)
        {
            return SoViPhamCung.CompareTo(other.SoViPhamCung);
        }

        // 2. Cùng số vi phạm cứng -> so sánh tổng điểm phạt mềm
        int cmpMem = TongDiemPhatMem.CompareTo(other.TongDiemPhatMem);
        if (cmpMem != 0) return cmpMem;

        // 3. Nếu điểm phạt mềm bằng nhau -> so sánh tổng điểm phạt
        return DiemPhat.CompareTo(other.DiemPhat);
    }
}

