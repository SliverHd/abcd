namespace XepLichGiangDay.Api.Services.ThuatToan.Genetic;

public class DotBien
{
    private readonly Random _random = new();

    /// <summary>
    /// Đột biến thay thế ngẫu nhiên (Random Resetting Mutation):
    /// Duyệt qua từng gen, với xác suất tyLeDotBien, thay thế GV hiện tại bằng một GV khác có chuyên môn.
    /// </summary>
    public CaThe ThucHien(CaThe caThe, double tyLeDotBien, Dictionary<int, List<int>> ungVienTheoLop, HashSet<int>? cacLopCoDinh = null)
    {
        foreach (var gen in caThe.DanhSachGen)
        {
            // Không đột biến lớp đã ghim / khóa
            if (cacLopCoDinh != null && cacLopCoDinh.Contains(gen.MaLopHocPhan))
            {
                continue;
            }

            if (_random.NextDouble() < tyLeDotBien)
            {
                if (ungVienTheoLop.TryGetValue(gen.MaLopHocPhan, out var dsUngVien) && dsUngVien.Count > 1)
                {
                    // Chọn một GV khác với GV hiện tại
                    var ungVienKhac = dsUngVien.Where(x => x != gen.MaGiangVien).ToList();
                    if (ungVienKhac.Count > 0)
                    {
                        gen.MaGiangVien = ungVienKhac[_random.Next(ungVienKhac.Count)];
                    }
                }
            }
        }

        return caThe;
    }
}
