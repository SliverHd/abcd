namespace XepLichGiangDay.Api.Services.ThuatToan.Genetic;

public class ThoiGianLop
{
    public int MaLopHocPhan { get; set; }
    public int Thu { get; set; }
    public int TietBatDau { get; set; }
    public int TietKetThuc { get; set; }
    public int TuTuan { get; set; } = 1;
    public int DenTuan { get; set; } = 53;

    public bool TrungGioVoi(ThoiGianLop other)
    {
        if (Thu != other.Thu) return false;
        bool giaoTuan = Math.Max(TuTuan, other.TuTuan) <= Math.Min(DenTuan, other.DenTuan);
        if (!giaoTuan) return false;
        return !(TietKetThuc < other.TietBatDau || TietBatDau > other.TietKetThuc);
    }
}

public class SuaCaThe
{
    private readonly Random _random = new();

    /// <summary>
    /// Toán tử sửa cá thể (Repair Operator):
    /// Quét các lớp trùng lịch của cùng một giảng viên và gán lại cho giảng viên khác không bị trùng.
    /// </summary>
    public CaThe SuaNeuViPham(
        CaThe caThe,
        Dictionary<int, List<ThoiGianLop>> thoiGianTheoLop,
        Dictionary<int, List<int>> ungVienTheoLop,
        HashSet<int>? cacLopCoDinh = null)
    {
        // Nhóm các lớp theo giảng viên được phân công
        var lopTheoGv = new Dictionary<int, List<int>>();
        foreach (var gen in caThe.DanhSachGen)
        {
            if (gen.MaGiangVien.HasValue && gen.MaGiangVien.Value > 0)
            {
                int gvId = gen.MaGiangVien.Value;
                if (!lopTheoGv.ContainsKey(gvId))
                {
                    lopTheoGv[gvId] = [];
                }
                lopTheoGv[gvId].Add(gen.MaLopHocPhan);
            }
        }

        // Kiểm tra từng giảng viên xem có bị trùng giờ không
        foreach (var (gvId, dsLop) in lopTheoGv)
        {
            if (dsLop.Count <= 1) continue;

            for (int i = 0; i < dsLop.Count; i++)
            {
                for (int j = i + 1; j < dsLop.Count; j++)
                {
                    int lop1 = dsLop[i];
                    int lop2 = dsLop[j];

                    if (thoiGianTheoLop.TryGetValue(lop1, out var tg1) &&
                        thoiGianTheoLop.TryGetValue(lop2, out var tg2))
                    {
                        bool isConflict = tg1.Any(t1 => tg2.Any(t2 => t1.TrungGioVoi(t2)));
                        if (isConflict)
                        {
                            // Ưu tiên đổi lớp chưa bị khóa (nếu lop2 bị khóa mà lop1 không khóa thì đổi lop1)
                            int lopDoi = (cacLopCoDinh != null && cacLopCoDinh.Contains(lop2) && !cacLopCoDinh.Contains(lop1)) ? lop1 : lop2;

                            if (cacLopCoDinh == null || !cacLopCoDinh.Contains(lopDoi))
                            {
                                var genDoi = caThe.DanhSachGen.FirstOrDefault(g => g.MaLopHocPhan == lopDoi);
                                if (genDoi != null && ungVienTheoLop.TryGetValue(lopDoi, out var dsUngVien))
                                {
                                    var thayThe = dsUngVien.Where(x => x != gvId).ToList();
                                    if (thayThe.Count > 0)
                                    {
                                        genDoi.MaGiangVien = thayThe[_random.Next(thayThe.Count)];
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return caThe;
    }
}
