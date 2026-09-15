import { useEffect, useState, type FormEvent } from 'react'
import { hocKyApi } from '../../api/hocKyApi'
import { giangVienApi } from '../../api/giangVienApi'
import { nguyenVongApi } from '../../api/nguyenVongApi'
import { useAuth } from '../../contexts/AuthContext'
import Loading from '../../components/Loading'
import type { HocKy } from '../../types/hocKy'
import type { GiangVien } from '../../types/giangVien'
import {
  DANH_MUC_RANG_BUOC_MEM,
  type NguyenVong,
  type ThemNguyenVongRequest,
} from '../../types/nguyenVong'

const CAC_THU = [
  { id: 2, ten: 'Thứ 2' },
  { id: 3, ten: 'Thứ 3' },
  { id: 4, ten: 'Thứ 4' },
  { id: 5, ten: 'Thứ 5' },
  { id: 6, ten: 'Thứ 6' },
  { id: 7, ten: 'Thứ 7' },
  { id: 8, ten: 'Chủ nhật' },
]

function DanhSachNguyenVong() {
  const { user } = useAuth()
  const laTruongBoMon = user?.vaiTro === 'TruongBoMon'
  const laGiangVien = user?.vaiTro === 'GiangVien'

  const [danhSachHocKy, setDanhSachHocKy] = useState<HocKy[]>([])
  const [danhSachGiangVien, setDanhSachGiangVien] = useState<GiangVien[]>([])
  const [maHocKy, setMaHocKy] = useState<number>(0)
  const [boLocGiangVien, setBoLocGiangVien] = useState<number>(0)

  // Danh sách nguyện vọng lấy từ backend
  const [danhSachNguyenVong, setDanhSachNguyenVong] = useState<NguyenVong[]>([])

  // Ẩn/hiện form thêm nguyện vọng (Trưởng bộ môn mặc định chỉ xem danh sách nhận được)
  const [hienFormThem, setHienFormThem] = useState<boolean>(!laTruongBoMon)

  // State Form nhập liệu
  const [form, setForm] = useState<ThemNguyenVongRequest>({
    maHocKy: 0,
    maGiangVien: user?.maGiangVien ?? 0,
    maRangBuocMem: 'SC01',
    tenRangBuocMem: DANH_MUC_RANG_BUOC_MEM[0].tenRb,
    thu: 2,
    tietBatDau: 1,
    tietKetThuc: 5,
    trongSoPhat: DANH_MUC_RANG_BUOC_MEM[0].diemPhatMacDinh,
    lyDo: '',
  })

  const [dangTai, setDangTai] = useState(false)
  const [dangLuu, setDangLuu] = useState(false)
  const [thongBao, setThongBao] = useState('')
  const [loi, setLoi] = useState('')

  // 1. Tải danh mục Học kỳ và Giảng viên
  useEffect(() => {
    const khoiTao = async () => {
      try {
        setDangTai(true)
        const [resHK, resGV] = await Promise.all([
          hocKyApi.getAll(),
          giangVienApi.getAll(),
        ])
        setDanhSachHocKy(resHK.data)
        setDanhSachGiangVien(resGV.data)

        if (resHK.data.length > 0) {
          const hkDau = resHK.data[0].maHocKy
          setMaHocKy(hkDau)
          setForm((prev) => ({ ...prev, maHocKy: hkDau }))
        }

        // Nếu là giảng viên: lọc đúng theo giảng viên đó
        if (laGiangVien && user?.maGiangVien) {
          setBoLocGiangVien(user.maGiangVien)
          setForm((prev) => ({ ...prev, maGiangVien: user.maGiangVien! }))
        } else if (resGV.data.length > 0) {
          const gvDau = resGV.data[0].maGiangVien
          setForm((prev) => ({ ...prev, maGiangVien: gvDau }))
        }
      } catch {
        setLoi('Không thể tải danh mục học kỳ hoặc giảng viên.')
      } finally {
        setDangTai(false)
      }
    }
    void khoiTao()
  }, [laGiangVien, user?.maGiangVien])

  // 2. Tải danh sách nguyện vọng khi chọn học kỳ hoặc đổi bộ lọc GV
  useEffect(() => {
    if (maHocKy > 0) {
      const gvId = laGiangVien && user?.maGiangVien ? user.maGiangVien : boLocGiangVien
      void taiDanhSachNguyenVong(maHocKy, gvId)
      setForm((prev) => ({ ...prev, maHocKy }))
    }
  }, [maHocKy, boLocGiangVien, laGiangVien, user?.maGiangVien])

  const taiDanhSachNguyenVong = async (hkId: number, gvId?: number) => {
    try {
      setDangTai(true)
      const res = await nguyenVongApi.getNguyenVong(hkId, gvId && gvId > 0 ? gvId : undefined)
      setDanhSachNguyenVong(res.data)
    } catch {
      setLoi('Không thể tải danh sách nguyện vọng.')
    } finally {
      setDangTai(false)
    }
  }

  // Thay đổi loại ràng buộc mềm trên form
  const handleThayDoiRangBuoc = (maRb: string) => {
    const rbChon = DANH_MUC_RANG_BUOC_MEM.find((x) => x.maRb === maRb)
    if (!rbChon) return

    setForm((prev) => ({
      ...prev,
      maRangBuocMem: rbChon.maRb,
      tenRangBuocMem: rbChon.tenRb,
      trongSoPhat: rbChon.diemPhatMacDinh,
      thu: rbChon.thuMacDinh ?? prev.thu,
      tietBatDau: rbChon.tietBdMacDinh ?? prev.tietBatDau,
      tietKetThuc: rbChon.tietKtMacDinh ?? prev.tietKetThuc,
    }))
  }

  // Thêm mới nguyện vọng ràng buộc mềm
  const handleThemNguyenVong = async (e: FormEvent) => {
    e.preventDefault()
    setThongBao('')
    setLoi('')

    const gvId = laGiangVien && user?.maGiangVien ? user.maGiangVien : form.maGiangVien
    if (!gvId || gvId <= 0) {
      setLoi('Vui lòng chọn giảng viên.')
      return
    }

    if (!form.tietBatDau || !form.tietKetThuc || form.tietBatDau > form.tietKetThuc) {
      setLoi('Tiết bắt đầu không được lớn hơn tiết kết thúc.')
      return
    }

    try {
      setDangLuu(true)
      await nguyenVongApi.themNguyenVong({
        ...form,
        maGiangVien: gvId,
        maHocKy,
      })

      setThongBao('Đã gửi nguyện vọng / ràng buộc mềm thành công.')
      setForm((prev) => ({ ...prev, lyDo: '' }))
      const gvLoc = laGiangVien && user?.maGiangVien ? user.maGiangVien : boLocGiangVien
      await taiDanhSachNguyenVong(maHocKy, gvLoc)
    } catch {
      setLoi('Không thể thêm nguyện vọng. Vui lòng kiểm tra lại dữ liệu.')
    } finally {
      setDangLuu(false)
    }
  }

  // Xóa nguyện vọng
  const handleXoaNguyenVong = async (id: number) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa nguyện vọng này?')) return

    try {
      await nguyenVongApi.xoaNguyenVong(id)
      setThongBao('Đã xóa nguyện vọng thành công.')
      const gvLoc = laGiangVien && user?.maGiangVien ? user.maGiangVien : boLocGiangVien
      await taiDanhSachNguyenVong(maHocKy, gvLoc)
    } catch {
      setLoi('Lỗi khi xóa nguyện vọng.')
    }
  }

  // Thống kê số giảng viên đã gửi nguyện vọng
  const soGvDaGui = new Set(danhSachNguyenVong.map((x) => x.maGiangVien)).size

  return (
    <div className="container-fluid py-2">
      {/* Tiêu đề trang theo vai trò */}
      <div className="mb-3 d-flex justify-content-between align-items-center">
        <div>
          <h2 className="h4 mb-1">
            {laTruongBoMon
              ? 'Danh sách Nguyện vọng Giảng viên gửi đến'
              : laGiangVien
              ? 'Khai báo Nguyện vọng & Ràng buộc mềm của tôi'
              : 'Quản lý Nguyện vọng & Ràng buộc mềm Giảng viên'}
          </h2>
          <p className="text-muted small mb-0">
            {laTruongBoMon
              ? 'Trưởng bộ môn theo dõi và duyệt các nguyện vọng ca dạy / ràng buộc mềm do giảng viên gửi lên theo học kỳ'
              : 'Khai báo thông tin lỗi ràng buộc mềm, thông số phạt, lý do và thời gian áp dụng theo từng giảng viên'}
          </p>
        </div>

        {/* Nút bật/tắt form khai báo hộ cho Trưởng bộ môn */}
        {laTruongBoMon && (
          <button
            type="button"
            className="btn btn-outline-secondary btn-sm"
            onClick={() => setHienFormThem(!hienFormThem)}
          >
            {hienFormThem ? '✕ Đóng form khai báo' : '+ Khai báo hộ giảng viên'}
          </button>
        )}
      </div>

      {thongBao && <div className="alert alert-success py-2 small mb-3">{thongBao}</div>}
      {loi && <div className="alert alert-danger py-2 small mb-3">{loi}</div>}

      {/* Thanh chọn Học kỳ, Bộ lọc & Tóm tắt thống kê của Trưởng bộ môn */}
      <div className="row g-2 mb-3 align-items-center bg-light p-2 border">
        <div className="col-auto">
          <label className="col-form-label col-form-label-sm fw-semibold">Học kỳ:</label>
        </div>
        <div className="col-md-3">
          <select
            className="form-select form-select-sm"
            value={maHocKy}
            onChange={(e) => setMaHocKy(Number(e.target.value))}
          >
            {danhSachHocKy.map((hk) => (
              <option key={hk.maHocKy} value={hk.maHocKy}>
                {hk.tenHocKy}
              </option>
            ))}
          </select>
        </div>

        {/* Bộ lọc Giảng viên (chỉ hiển thị cho Trưởng bộ môn / Admin) */}
        {!laGiangVien && (
          <>
            <div className="col-auto ms-md-3">
              <label className="col-form-label col-form-label-sm fw-semibold">Lọc theo GV:</label>
            </div>
            <div className="col-md-3">
              <select
                className="form-select form-select-sm"
                value={boLocGiangVien}
                onChange={(e) => setBoLocGiangVien(Number(e.target.value))}
              >
                <option value={0}>-- Tất cả giảng viên ({soGvDaGui} GV đã gửi) --</option>
                {danhSachGiangVien.map((gv) => (
                  <option key={gv.maGiangVien} value={gv.maGiangVien}>
                    {gv.hoTen} ({gv.email || `Mã: ${gv.maGiangVien}`})
                  </option>
                ))}
              </select>
            </div>
          </>
        )}

        {/* Khối thống kê nhanh cho Trưởng bộ môn */}
        {laTruongBoMon && (
          <div className="col text-end small text-muted">
            Tổng số GV đã gửi: <strong className="text-primary">{soGvDaGui}</strong> | Tổng số nguyện vọng nhận: <strong className="text-danger">{danhSachNguyenVong.length}</strong>
          </div>
        )}
      </div>

      {/* Form khai báo nguyện vọng / ràng buộc mềm (hiển thị khi hienFormThem = true hoặc là Giảng viên) */}
      {hienFormThem && (
        <div className="border bg-white p-3 mb-4">
          <div className="fw-semibold mb-3 text-dark border-bottom pb-2 d-flex justify-content-between">
            <span>
              {laGiangVien
                ? 'Gửi nguyện vọng / Ràng buộc mềm mới đến Trưởng bộ môn'
                : 'Khai báo nguyện vọng / Ràng buộc mềm cho giảng viên'}
            </span>
            {laTruongBoMon && (
              <button
                type="button"
                className="btn-close btn-sm"
                aria-label="Close"
                onClick={() => setHienFormThem(false)}
              ></button>
            )}
          </div>

          <form onSubmit={handleThemNguyenVong}>
            <div className="row g-2 mb-2">
              {/* Giảng viên */}
              <div className="col-md-4">
                <label className="form-label small fw-semibold">
                  Giảng viên <span className="text-danger">*</span>
                </label>
                {laGiangVien ? (
                  <input
                    type="text"
                    className="form-control form-control-sm bg-light"
                    value={`${user?.hoTen || 'Tôi'} (Mã GV #${user?.maGiangVien})`}
                    disabled
                  />
                ) : (
                  <select
                    className="form-select form-select-sm"
                    value={form.maGiangVien}
                    onChange={(e) => setForm({ ...form, maGiangVien: Number(e.target.value) })}
                    required
                  >
                    {danhSachGiangVien.map((gv) => (
                      <option key={gv.maGiangVien} value={gv.maGiangVien}>
                        [GV #{gv.maGiangVien}] {gv.hoTen}
                      </option>
                    ))}
                  </select>
                )}
              </div>

              {/* Lỗi / Mã ràng buộc mềm */}
              <div className="col-md-5">
                <label className="form-label small fw-semibold">
                  Lỗi / Mã ràng buộc mềm <span className="text-danger">*</span>
                </label>
                <select
                  className="form-select form-select-sm"
                  value={form.maRangBuocMem}
                  onChange={(e) => handleThayDoiRangBuoc(e.target.value)}
                >
                  {DANH_MUC_RANG_BUOC_MEM.map((rb) => (
                    <option key={rb.maRb} value={rb.maRb}>
                      [{rb.maRb}] {rb.tenRb} (Phạt mặc định: {rb.diemPhatMacDinh})
                    </option>
                  ))}
                </select>
              </div>

              {/* Thông số phạt */}
              <div className="col-md-3">
                <label className="form-label small fw-semibold">Thông số phạt</label>
                <input
                  type="number"
                  step="0.5"
                  min="0"
                  className="form-control form-control-sm"
                  value={form.trongSoPhat}
                  onChange={(e) => setForm({ ...form, trongSoPhat: Number(e.target.value) })}
                  required
                />
              </div>
            </div>

            <div className="row g-2 mb-3">
              {/* Thứ */}
              <div className="col-md-2">
                <label className="form-label small fw-semibold">Thứ</label>
                <select
                  className="form-select form-select-sm"
                  value={form.thu ?? 2}
                  onChange={(e) => setForm({ ...form, thu: Number(e.target.value) })}
                >
                  {CAC_THU.map((thu) => (
                    <option key={thu.id} value={thu.id}>
                      {thu.ten}
                    </option>
                  ))}
                </select>
              </div>

              {/* Tiết bắt đầu */}
              <div className="col-md-2">
                <label className="form-label small fw-semibold">Thời gian bắt đầu (Tiết)</label>
                <select
                  className="form-select form-select-sm"
                  value={form.tietBatDau ?? 1}
                  onChange={(e) => setForm({ ...form, tietBatDau: Number(e.target.value) })}
                >
                  {Array.from({ length: 12 }, (_, i) => i + 1).map((tiet) => (
                    <option key={tiet} value={tiet}>
                      Tiết {tiet}
                    </option>
                  ))}
                </select>
              </div>

              {/* Tiết kết thúc */}
              <div className="col-md-2">
                <label className="form-label small fw-semibold">Thời gian kết thúc (Tiết)</label>
                <select
                  className="form-select form-select-sm"
                  value={form.tietKetThuc ?? 5}
                  onChange={(e) => setForm({ ...form, tietKetThuc: Number(e.target.value) })}
                >
                  {Array.from({ length: 12 }, (_, i) => i + 1).map((tiet) => (
                    <option key={tiet} value={tiet}>
                      Tiết {tiet}
                    </option>
                  ))}
                </select>
              </div>

              {/* Lí do */}
              <div className="col-md-4">
                <label className="form-label small fw-semibold">Lí do / Ghi chú của GV</label>
                <input
                  type="text"
                  className="form-control form-control-sm"
                  placeholder="VD: Bận nghiên cứu, con nhỏ, đi công tác..."
                  value={form.lyDo ?? ''}
                  onChange={(e) => setForm({ ...form, lyDo: e.target.value })}
                />
              </div>

              {/* Nút thêm */}
              <div className="col-md-2 d-flex align-items-end">
                <button
                  type="submit"
                  className="btn btn-primary btn-sm w-100"
                  disabled={dangLuu}
                >
                  {dangLuu ? 'Đang gửi...' : laGiangVien ? 'Gửi nguyện vọng' : '+ Thêm nguyện vọng'}
                </button>
              </div>
            </div>
          </form>
        </div>
      )}

      {/* Bảng danh sách nguyện vọng & ràng buộc mềm */}
      <div className="border bg-white p-3">
        <div className="d-flex justify-content-between align-items-center mb-2">
          <div className="fw-semibold text-dark">
            {laTruongBoMon
              ? `Danh sách các nguyện vọng giảng viên gửi đến (${danhSachNguyenVong.length})`
              : laGiangVien
              ? `Các nguyện vọng tôi đã gửi đi (${danhSachNguyenVong.length})`
              : `Danh sách nguyện vọng & ràng buộc mềm đã đăng ký (${danhSachNguyenVong.length})`}
          </div>
        </div>

        {dangTai ? (
          <Loading />
        ) : (
          <div className="table-responsive">
            <table className="table table-bordered table-sm table-hover mb-0">
              <thead className="table-light">
                <tr className="text-center align-middle">
                  <th style={{ width: '45px' }}>STT</th>
                  <th style={{ width: '80px' }}>Mã GV</th>
                  <th>Tên giảng viên</th>
                  <th style={{ width: '90px' }}>Mã RB</th>
                  <th>Lỗi ràng buộc mềm</th>
                  <th style={{ width: '180px' }}>Thời gian bắt đầu - kết thúc</th>
                  <th style={{ width: '110px' }}>Thông số phạt</th>
                  <th>Lí do giảng viên gửi</th>
                  <th style={{ width: '70px' }}>Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {danhSachNguyenVong.length === 0 ? (
                  <tr>
                    <td colSpan={9} className="text-center text-muted py-4">
                      {laTruongBoMon
                        ? 'Chưa có giảng viên nào gửi nguyện vọng trong học kỳ này.'
                        : laGiangVien
                        ? 'Bạn chưa gửi nguyện vọng nào cho học kỳ này.'
                        : 'Chưa có nguyện vọng hoặc ràng buộc mềm nào được đăng ký cho học kỳ này.'}
                    </td>
                  </tr>
                ) : (
                  danhSachNguyenVong.map((item, idx) => {
                    const tenThu = CAC_THU.find((t) => t.id === item.thu)?.ten || `Thứ ${item.thu}`
                    return (
                      <tr key={item.maNguyenVong} className="align-middle">
                        <td className="text-center">{idx + 1}</td>
                        <td className="text-center font-monospace small">GV #{item.maGiangVien}</td>
                        <td className="fw-semibold">{item.tenGiangVien || `Giảng viên #${item.maGiangVien}`}</td>
                        <td className="text-center">
                          <span className="badge bg-secondary">{item.maRangBuocMem}</span>
                        </td>
                        <td>{item.tenRangBuocMem || item.loaiNguyenVong}</td>
                        <td className="text-center">
                          {item.thu && item.tietBatDau && item.tietKetThuc ? (
                            <span>
                              {tenThu}: Tiết {item.tietBatDau} - {item.tietKetThuc}
                            </span>
                          ) : (
                            <span className="text-muted">Cả học kỳ</span>
                          )}
                        </td>
                        <td className="text-center text-danger fw-semibold">
                          +{item.trongSoPhat}
                        </td>
                        <td className="small">{item.lyDo || item.noiDungGoc || '—'}</td>
                        <td className="text-center">
                          <button
                            type="button"
                            className="btn btn-outline-danger btn-sm py-0 px-2"
                            title="Xóa nguyện vọng"
                            onClick={() => handleXoaNguyenVong(item.maNguyenVong)}
                          >
                            Xóa
                          </button>
                        </td>
                      </tr>
                    )
                  })
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}

export default DanhSachNguyenVong
