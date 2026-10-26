namespace XepLichGiangDay.Api.Services.ThuatToan.Genetic;

public class DotBien
{
    private readonly Random _random;

    public DotBien(Random? random = null)
    {
        _random = random ?? new Random();
    }

    /// <summary>
    /// Đột biến hỗn hợp (Hybrid Mutation):
    /// 1. Đột biến thay thế ngẫu nhiên (Random Resetting Mutation): Thay thế GV bằng ứng viên chuyên môn khác.
    /// 2. Đột biến hoán đổi (Swap Mutation): Hoán đổi GV giữa 2 lớp nếu cả 2 đều đủ năng lực chuyên môn cho lớp của nhau.
    /// Không bao giờ đột biến các lớp đã bị khóa ghim cố định.
    /// </summary>
    public CaThe ThucHien(
        CaThe caThe,
        double tyLeDotBien,
        Dictionary<int, List<int>> ungVienTheoLop,
        HashSet<int>? cacLopCoDinh = null)
    {
        if (caThe == null || caThe.DanhSachGen.Count == 0) return caThe!;

        // 1. Đột biến thay thế ngẫu nhiên trên từng gen
        foreach (var gen in caThe.DanhSachGen)
        {
            if (cacLopCoDinh != null && cacLopCoDinh.Contains(gen.MaLopHocPhan))
            {
                continue;
            }

            if (_random.NextDouble() < tyLeDotBien)
            {
                if (ungVienTheoLop.TryGetValue(gen.MaLopHocPhan, out var dsUngVien) && dsUngVien.Count > 1)
                {
                    var ungVienKhac = dsUngVien.Where(x => x != gen.MaGiangVien).ToList();
                    if (ungVienKhac.Count > 0)
                    {
                        gen.MaGiangVien = ungVienKhac[_random.Next(ungVienKhac.Count)];
                    }
                }
            }
        }

        // 2. Đột biến hoán đổi (Swap Mutation) với xác suất phụ = tyLeDotBien * 0.5
        if (caThe.DanhSachGen.Count >= 2 && _random.NextDouble() < (tyLeDotBien * 0.5))
        {
            int idx1 = _random.Next(caThe.DanhSachGen.Count);
            int idx2 = _random.Next(caThe.DanhSachGen.Count);

            if (idx1 != idx2)
            {
                var g1 = caThe.DanhSachGen[idx1];
                var g2 = caThe.DanhSachGen[idx2];

                bool g1Khoa = cacLopCoDinh != null && cacLopCoDinh.Contains(g1.MaLopHocPhan);
                bool g2Khoa = cacLopCoDinh != null && cacLopCoDinh.Contains(g2.MaLopHocPhan);

                if (!g1Khoa && !g2Khoa && g1.MaGiangVien.HasValue && g2.MaGiangVien.HasValue)
                {
                    int gv1 = g1.MaGiangVien.Value;
                    int gv2 = g2.MaGiangVien.Value;

                    if (gv1 != gv2)
                    {
                        // Kiểm tra tính tương thích chuyên môn 2 chiều
                        bool gv1DayDuocLop2 = ungVienTheoLop.TryGetValue(g2.MaLopHocPhan, out var uv2) && uv2.Contains(gv1);
                        bool gv2DayDuocLop1 = ungVienTheoLop.TryGetValue(g1.MaLopHocPhan, out var uv1) && uv1.Contains(gv2);

                        if (gv1DayDuocLop2 && gv2DayDuocLop1)
                        {
                            g1.MaGiangVien = gv2;
                            g2.MaGiangVien = gv1;
                        }
                    }
                }
            }
        }

        return caThe;
    }
}


