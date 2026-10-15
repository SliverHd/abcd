import { useEffect, useState } from 'react'
import { hocKyApi } from '../../api/hocKyApi'
import { giangVienApi } from '../../api/giangVienApi'
import { nguyenVongApi } from '../../api/nguyenVongApi'
import Loading from '../../components/Loading'
import type { HocKy } from '../../types/hocKy'
import type { GiangVien } from '../../types/giangVien'
import type {
  TaoLichBanDto,
  TaoNguyenVongDto,
  TrangThaiTiet,
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

const CAC_TIET = Array.from({ length: 12 }, (_, i) => i + 1)

function DanhSachNguyenVong() {
  const [danhSachHocKy, setDanhSachHocKy] = useState<HocKy[]>([])
  const [danhSachGiangVien, setDanhSachGiangVien] = useState<GiangVien[]>([])
  const [maHocKy, setMaHocKy] = useState<number>(0)
  const [maGiangVien, setMaGiangVien] = useState<number>(0)

  // Ma trận trạng thái: key dạng `${thu}_${tiet}`
  const [maTran, setMaTran] = useState<Record<string, TrangThaiTiet>>({})
  const [lyDoBan, setLyDoBan] = useState<string>('')
  const [cheDoChon, setCheDoChon] = useState<TrangThaiTiet>('BUSY')

  const [dangTai, setDangTai] = useState(false)
  const [dangLuu, setDangLuu] = useState(false)
  const [thongBao, setThongBao] = useState('')
  const [loi, setLoi] = useState('')

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
          setMaHocKy(resHK.data[0].maHocKy)
        }
        if (resGV.data.length > 0) {
          setMaGiangVien(resGV.data[0].maGiangVien)
        }
      } catch {
        setLoi('Không thể tải danh mục học kỳ hoặc giảng viên.')
      } finally {
        setDangTai(false)
      }
    }
    void khoiTao()
  }, [])

  useEffect(() => {
    if (maHocKy > 0 && maGiangVien > 0) {
      void taiDuLieuMaTran(maHocKy, maGiangVien)
    }
  }, [maHocKy, maGiangVien])

  const taiDuLieuMaTran = async (hkId: number, gvId: number) => {
    try {
      setDangTai(true)
      setThongBao('')
      setLoi('')

      const [resLB, resNV] = await Promise.all([
        nguyenVongApi.getLichBan(hkId, gvId),
        nguyenVongApi.getNguyenVong(hkId, gvId),
      ])

      const newMatrix: Record<string, TrangThaiTiet> = {}

      // Điền lịch bận (BUSY)
      resLB.data.forEach((lb) => {
        for (let t = lb.tietBatDau; t <= lb.tietKetThuc; t++) {
          newMatrix[`${lb.thu}_${t}`] = 'BUSY'
        }
      })

      // Điền nguyện vọng (PREFERRED / AVOID)
      resNV.data.forEach((nv) => {
        if (nv.thu && nv.tietBatDau) {
          const tkt = nv.tietKetThuc || nv.tietBatDau
          for (let t = nv.tietBatDau; t <= tkt; t++) {
            newMatrix[`${nv.thu}_${t}`] = nv.loaiNguyenVong === 'TranhDay' ? 'AVOID' : 'PREFERRED'
          }
        }
      })

      setMaTran(newMatrix)
    } catch {
      setLoi('Không thể tải lịch bận / nguyện vọng của giảng viên.')
    } finally {
      setDangTai(false)
    }
  }

  const handleOClick = (thu: number, tiet: number) => {
    const key = `${thu}_${tiet}`
    const current = maTran[key] || 'FREE'

    // Nếu đang chọn ô cùng trạng thái với chế độ chọn hiện tại thì gán lại FREE
    const nextStatus: TrangThaiTiet = current === cheDoChon ? 'FREE' : cheDoChon

    setMaTran((prev) => {
      const copy = { ...prev }
      if (nextStatus === 'FREE') {
        delete copy[key]
      } else {
        copy[key] = nextStatus
      }
      return copy
    })
  }

  const handleChonNhanhCa = (thu: number, tietDau: number, tietCuoi: number) => {
    setMaTran((prev) => {
      const copy = { ...prev }
      for (let t = tietDau; t <= tietCuoi; t++) {
        const key = `${thu}_${t}`
        if (cheDoChon === 'FREE') {
          delete copy[key]
        } else {
          copy[key] = cheDoChon
        }
      }
      return copy
    })
  }

  const handleXoaTatCa = () => {
    if (confirm('Bạn có chắc muốn xóa tất cả thiết lập lịch của giảng viên này?')) {
      setMaTran({})
    }
  }

  const handleLuu = async () => {
    if (maHocKy <= 0 || maGiangVien <= 0) {
      alert('Vui lòng chọn học kỳ và giảng viên!')
      return
    }

    try {
      setDangLuu(true)
      setThongBao('')
      setLoi('')

      const danhSachLichBan: TaoLichBanDto[] = []
      const danhSachNguyenVong: TaoNguyenVongDto[] = []

      // Gom các tiết liên tục cùng thứ và cùng trạng thái
      for (const thuObj of CAC_THU) {
        const thu = thuObj.id
        let startTiet = -1
        let currentStatus: TrangThaiTiet = 'FREE'

        for (let t = 1; t <= 12; t++) {
          const status = maTran[`${thu}_${t}`] || 'FREE'

          if (status !== currentStatus) {
            if (currentStatus !== 'FREE' && startTiet > 0) {
              const endTiet = t - 1
              if (currentStatus === 'BUSY') {
                danhSachLichBan.push({
                  thu,
                  tietBatDau: startTiet,
                  tietKetThuc: endTiet,
                  lyDo: lyDoBan || 'Bận cá nhân / Seminar',
                })
              } else if (currentStatus === 'PREFERRED') {
                danhSachNguyenVong.push({
                  loaiNguyenVong: 'ThichDay',
                  mucDo: 2,
                  thu,
                  tietBatDau: startTiet,
                  tietKetThuc: endTiet,
                  ghiChu: 'Thích dạy',
                })
              } else if (currentStatus === 'AVOID') {
                danhSachNguyenVong.push({
                  loaiNguyenVong: 'TranhDay',
                  mucDo: 1,
                  thu,
                  tietBatDau: startTiet,
                  tietKetThuc: endTiet,
                  ghiChu: 'Tránh dạy',
                })
              }
            }
            currentStatus = status
            startTiet = status !== 'FREE' ? t : -1
          }
        }

        // Xử lý tiết cuối cùng 12
        if (currentStatus !== 'FREE' && startTiet > 0) {
          if (currentStatus === 'BUSY') {
            danhSachLichBan.push({
              thu,
              tietBatDau: startTiet,
              tietKetThuc: 12,
              lyDo: lyDoBan || 'Bận cá nhân / Seminar',
            })
          } else if (currentStatus === 'PREFERRED') {
            danhSachNguyenVong.push({
              loaiNguyenVong: 'ThichDay',
              mucDo: 2,
              thu,
              tietBatDau: startTiet,
              tietKetThuc: 12,
              ghiChu: 'Thích dạy',
            })
          } else if (currentStatus === 'AVOID') {
            danhSachNguyenVong.push({
              loaiNguyenVong: 'TranhDay',
              mucDo: 1,
              thu,
              tietBatDau: startTiet,
              tietKetThuc: 12,
              ghiChu: 'Tránh dạy',
            })
          }
        }
      }

      await nguyenVongApi.luuMaTran({
        maHocKy,
        maGiangVien,
        danhSachLichBan,
        danhSachNguyenVong,
      })

      setThongBao('Đã lưu thành công ma trận lịch bận & nguyện vọng cho giảng viên!')
    } catch {
      setLoi('Lỗi khi lưu dữ liệu lên máy chủ.')
    } finally {
      setDangLuu(false)
    }
  }

  // Thống kê số tiết
  const soTietBusy = Object.values(maTran).filter((v) => v === 'BUSY').length
  const soTietPreferred = Object.values(maTran).filter((v) => v === 'PREFERRED').length
  const soTietAvoid = Object.values(maTran).filter((v) => v === 'AVOID').length

  const getCellColor = (status?: TrangThaiTiet) => {
    switch (status) {
      case 'BUSY':
        return '#ff4d4f' // Đỏ
      case 'PREFERRED':
        return '#52c41a' // Xanh lá
      case 'AVOID':
        return '#faad14' // Vàng cam
      default:
        return '#ffffff'
    }
  }

  const getCellText = (status?: TrangThaiTiet) => {
    switch (status) {
      case 'BUSY':
        return 'BẬN'
      case 'PREFERRED':
        return 'THÍCH'
      case 'AVOID':
        return 'TRÁNH'
      default:
        return ''
    }
  }

  return (
    <section>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h2 className="h4 mb-1">Ma trận Lịch bận & Nguyện vọng Giảng viên</h2>
          <p className="text-muted small mb-0">
            Khai báo lịch bận cố định (Ràng buộc cứng) và ca dạy mong muốn / xin tránh (Ràng buộc mềm)
          </p>
        </div>
        <button
          className="btn btn-primary"
          disabled={dangLuu || dangTai}
          onClick={handleLuu}
        >
          {dangLuu ? 'Đang lưu...' : '💾 Lưu ma trận đăng ký'}
        </button>
      </div>

      {/* Bộ chọn Học kỳ & Giảng viên */}
      <div className="card shadow-sm p-3 mb-3 bg-light">
        <div className="row g-3 align-items-end">
          <div className="col-md-4">
            <label className="form-label fw-bold">Học kỳ</label>
            <select
              className="form-select"
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
          <div className="col-md-4">
            <label className="form-label fw-bold">Giảng viên</label>
            <select
              className="form-select"
              value={maGiangVien}
              onChange={(e) => setMaGiangVien(Number(e.target.value))}
            >
              {danhSachGiangVien.map((gv) => (
                <option key={gv.maGiangVien} value={gv.maGiangVien}>
                  {gv.hoTen} {gv.tenBoMon ? `(${gv.tenBoMon})` : ''}
                </option>
              ))}
            </select>
          </div>
          <div className="col-md-4">
            <label className="form-label small text-muted">Lý do bận (nếu có)</label>
            <input
              type="text"
              className="form-control"
              placeholder="VD: Seminar bộ môn, Nghiên cứu..."
              value={lyDoBan}
              onChange={(e) => setLyDoBan(e.target.value)}
            />
          </div>
        </div>
      </div>

      {/* Thanh công cụ chọn cọ vẽ */}
      <div className="d-flex flex-wrap align-items-center justify-content-between p-2 mb-3 border rounded bg-white shadow-sm">
        <div className="d-flex align-items-center gap-2">
          <span className="fw-bold small text-muted me-1">Chế độ đánh dấu:</span>
          <button
            type="button"
            className={`btn btn-sm ${cheDoChon === 'BUSY' ? 'btn-danger' : 'btn-outline-danger'}`}
            onClick={() => setCheDoChon('BUSY')}
          >
            ⛔ Lịch bận (Cứng)
          </button>
          <button
            type="button"
            className={`btn btn-sm ${cheDoChon === 'PREFERRED' ? 'btn-success' : 'btn-outline-success'}`}
            onClick={() => setCheDoChon('PREFERRED')}
          >
            ⭐ Thích dạy (+Ưu tiên)
          </button>
          <button
            type="button"
            className={`btn btn-sm ${cheDoChon === 'AVOID' ? 'btn-warning' : 'btn-outline-warning'}`}
            onClick={() => setCheDoChon('AVOID')}
          >
            ⚠️ Tránh dạy (-Phạt)
          </button>
          <button
            type="button"
            className={`btn btn-sm ${cheDoChon === 'FREE' ? 'btn-secondary' : 'btn-outline-secondary'}`}
            onClick={() => setCheDoChon('FREE')}
          >
            ⚪ Xóa / Rảnh
          </button>
        </div>

        <div className="d-flex gap-3 small align-items-center">
          <span>
            Bận: <strong className="text-danger">{soTietBusy}</strong> tiết
          </span>
          <span>
            Thích: <strong className="text-success">{soTietPreferred}</strong> tiết
          </span>
          <span>
            Tránh: <strong className="text-warning">{soTietAvoid}</strong> tiết
          </span>
          <button
            type="button"
            className="btn btn-sm btn-link text-danger text-decoration-none"
            onClick={handleXoaTatCa}
          >
            Xóa trắng
          </button>
        </div>
      </div>

      {dangTai && <Loading />}
      {thongBao && <div className="alert alert-success">{thongBao}</div>}
      {loi && <div className="alert alert-danger">{loi}</div>}

      {/* Ma trận Thời khóa biểu 12 tiết x 7 ngày */}
      {!dangTai && (
        <div className="card shadow-sm mb-4">
          <div className="table-responsive">
            <table className="table table-bordered text-center align-middle mb-0">
              <thead className="table-light">
                <tr>
                  <th style={{ width: '80px' }}>Buổi</th>
                  <th style={{ width: '70px' }}>Tiết</th>
                  {CAC_THU.map((thu) => (
                    <th key={thu.id}>{thu.ten}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {/* Buổi Sáng: Tiết 1 - 6 */}
                {CAC_TIET.map((tiet) => {
                  const buoiLabel =
                    tiet === 1
                      ? 'Sáng (1-6)'
                      : tiet === 7
                        ? 'Chiều (7-12)'
                        : null

                  return (
                    <tr key={tiet}>
                      {buoiLabel && (
                        <td
                          rowSpan={6}
                          className="fw-bold bg-light text-uppercase small"
                          style={{ verticalAlign: 'middle', borderRight: '2px solid #dee2e6' }}
                        >
                          {buoiLabel}
                        </td>
                      )}
                      <td className="fw-bold bg-light">Tiết {tiet}</td>
                      {CAC_THU.map((thu) => {
                        const status = maTran[`${thu.id}_${tiet}`]
                        const bgColor = getCellColor(status)
                        const text = getCellText(status)

                        return (
                          <td
                            key={`${thu.id}_${tiet}`}
                            style={{
                              backgroundColor: bgColor,
                              color: status ? '#ffffff' : '#6c757d',
                              fontWeight: 'bold',
                              fontSize: '11px',
                              cursor: 'pointer',
                              height: '40px',
                              userSelect: 'none',
                              transition: 'all 0.15s ease-in-out',
                            }}
                            onClick={() => handleOClick(thu.id, tiet)}
                            title={`Nhấp để đổi trạng thái Thứ ${thu.id} Tiết ${tiet}`}
                          >
                            {text}
                          </td>
                        )
                      })}
                    </tr>
                  )
                })}
              </tbody>
              <tfoot className="table-light">
                <tr>
                  <td colSpan={2}>
                    <small className="fw-bold">Chọn nhanh ca:</small>
                  </td>
                  {CAC_THU.map((thu) => (
                    <td key={thu.id}>
                      <div className="btn-group btn-group-sm">
                        <button
                          type="button"
                          className="btn btn-outline-secondary py-0 px-1"
                          style={{ fontSize: '10px' }}
                          title={`Áp dụng cho Thứ ${thu.id} cả sáng (1-6)`}
                          onClick={() => handleChonNhanhCa(thu.id, 1, 6)}
                        >
                          Sáng
                        </button>
                        <button
                          type="button"
                          className="btn btn-outline-secondary py-0 px-1"
                          style={{ fontSize: '10px' }}
                          title={`Áp dụng cho Thứ ${thu.id} cả chiều (7-12)`}
                          onClick={() => handleChonNhanhCa(thu.id, 7, 12)}
                        >
                          Chiều
                        </button>
                      </div>
                    </td>
                  ))}
                </tr>
              </tfoot>
            </table>
          </div>
        </div>
      )}
    </section>
  )
}

export default DanhSachNguyenVong
