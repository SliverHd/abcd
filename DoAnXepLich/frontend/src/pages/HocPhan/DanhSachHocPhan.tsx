import { useEffect, useState } from 'react'
import { hocPhanApi } from '../../api/hocPhanApi'
import Loading from '../../components/Loading'
import type { HocPhan, TaoHocPhanDto } from '../../types/hocPhan'

function DanhSachHocPhan() {
  const [danhSach, setDanhSach] = useState<HocPhan[]>([])
  const [dangTai, setDangTai] = useState(true)
  const [loi, setLoi] = useState('')
  const [tuKhoa, setTuKhoa] = useState('')

  // State Modal Thêm / Sửa
  const [hienModal, setHienModal] = useState(false)
  const [dangSuaId, setDangSuaId] = useState<number | null>(null)
  const [formData, setFormData] = useState<TaoHocPhanDto>({
    tenHocPhan: '',
    soTinChi: 3,
    trangThai: 'DangApDung',
  })
  const [dangLuu, setDangLuu] = useState(false)

  const taiDuLieu = async () => {
    try {
      setDangTai(true)
      const res = await hocPhanApi.getAll()
      setDanhSach(res.data)
      setLoi('')
    } catch {
      setLoi('Không thể tải danh sách học phần.')
    } finally {
      setDangTai(false)
    }
  }

  useEffect(() => {
    void taiDuLieu()
  }, [])

  const handleMoModalThem = () => {
    setDangSuaId(null)
    setFormData({
      tenHocPhan: '',
      soTinChi: 3,
      trangThai: 'DangApDung',
    })
    setHienModal(true)
  }

  const handleMoModalSua = (hp: HocPhan) => {
    setDangSuaId(hp.maHocPhan)
    setFormData({
      tenHocPhan: hp.tenHocPhan,
      soTinChi: hp.soTinChi,
      trangThai: hp.trangThai || 'DangApDung',
    })
    setHienModal(true)
  }

  const handleLuu = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!formData.tenHocPhan.trim()) {
      alert('Vui lòng nhập tên học phần!')
      return
    }

    try {
      setDangLuu(true)
      if (dangSuaId) {
        await hocPhanApi.update(dangSuaId, formData)
      } else {
        await hocPhanApi.create(formData)
      }
      setHienModal(false)
      await taiDuLieu()
    } catch {
      alert('Không thể lưu học phần.')
    } finally {
      setDangLuu(false)
    }
  }

  const handleXoa = async (id: number, ten: string) => {
    if (!confirm(`Bạn có chắc chắn muốn xóa học phần "${ten}"?`)) return
    try {
      await hocPhanApi.delete(id)
      await taiDuLieu()
    } catch {
      alert('Không thể xóa học phần này (có thể đã được dùng trong Lớp học phần hoặc Năng lực giảng dạy).')
    }
  }

  const danhSachLoc = danhSach.filter((hp) =>
    hp.tenHocPhan.toLowerCase().includes(tuKhoa.toLowerCase())
  )

  return (
    <section>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h2 className="h4 mb-1">Danh mục Học phần (Môn học)</h2>
          <p className="text-muted small mb-0">
            Quản lý danh sách các học phần/môn học phục vụ phân công chuyên môn và thời khóa biểu
          </p>
        </div>
        <button className="btn btn-primary" onClick={handleMoModalThem}>
          + Thêm học phần
        </button>
      </div>

      <div className="row g-2 mb-3">
        <div className="col-md-6">
          <input
            type="text"
            className="form-control"
            placeholder="Tìm theo tên học phần..."
            value={tuKhoa}
            onChange={(e) => setTuKhoa(e.target.value)}
          />
        </div>
      </div>

      {dangTai && <Loading />}
      {loi && <div className="alert alert-warning">{loi}</div>}

      {!dangTai && !loi && (
        <div className="card shadow-sm">
          <div className="table-responsive">
            <table className="table table-hover align-middle mb-0">
              <thead className="table-light">
                <tr>
                  <th style={{ width: '60px' }}>Mã HP</th>
                  <th>Tên học phần</th>
                  <th style={{ width: '120px' }}>Số tín chỉ</th>
                  <th style={{ width: '140px' }}>Trạng thái</th>
                  <th style={{ width: '130px' }} className="text-center">Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {danhSachLoc.map((hp) => (
                  <tr key={hp.maHocPhan}>
                    <td>
                      <span className="badge bg-light text-dark border">#{hp.maHocPhan}</span>
                    </td>
                    <td>
                      <strong>{hp.tenHocPhan}</strong>
                    </td>
                    <td>
                      <span className="badge bg-info text-dark">{hp.soTinChi} TC</span>
                    </td>
                    <td>
                      <span
                        className={`badge ${
                          hp.trangThai === 'DangApDung' ? 'bg-success' : 'bg-secondary'
                        }`}
                      >
                        {hp.trangThai === 'DangApDung' ? 'Đang áp dụng' : hp.trangThai}
                      </span>
                    </td>
                    <td className="text-center">
                      <div className="btn-group btn-group-sm">
                        <button
                          className="btn btn-outline-secondary"
                          onClick={() => handleMoModalSua(hp)}
                        >
                          Sửa
                        </button>
                        <button
                          className="btn btn-outline-danger"
                          onClick={() => handleXoa(hp.maHocPhan, hp.tenHocPhan)}
                        >
                          Xóa
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
                {danhSachLoc.length === 0 && (
                  <tr>
                    <td colSpan={5} className="text-center text-secondary py-4">
                      Chưa có học phần nào.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Modal Thêm / Sửa */}
      {hienModal && (
        <div className="modal show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
          <div className="modal-dialog">
            <div className="modal-content">
              <form onSubmit={handleLuu}>
                <div className="modal-header">
                  <h5 className="modal-title">
                    {dangSuaId ? 'Cập nhật học phần' : 'Thêm học phần mới'}
                  </h5>
                  <button
                    type="button"
                    className="btn-close"
                    onClick={() => setHienModal(false)}
                  ></button>
                </div>
                <div className="modal-body">
                  <div className="mb-3">
                    <label className="form-label">
                      Tên học phần <span className="text-danger">*</span>
                    </label>
                    <input
                      type="text"
                      className="form-control"
                      required
                      value={formData.tenHocPhan}
                      onChange={(e) => setFormData({ ...formData, tenHocPhan: e.target.value })}
                      placeholder="VD: Cơ sở dữ liệu, Lập trình Web..."
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label">Số tín chỉ</label>
                    <input
                      type="number"
                      min={1}
                      max={10}
                      className="form-control"
                      value={formData.soTinChi}
                      onChange={(e) => setFormData({ ...formData, soTinChi: Number(e.target.value) })}
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label">Trạng thái</label>
                    <select
                      className="form-select"
                      value={formData.trangThai}
                      onChange={(e) => setFormData({ ...formData, trangThai: e.target.value })}
                    >
                      <option value="DangApDung">Đang áp dụng</option>
                      <option value="TamNgung">Tạm ngưng</option>
                    </select>
                  </div>
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={() => setHienModal(false)}
                  >
                    Hủy
                  </button>
                  <button type="submit" className="btn btn-primary" disabled={dangLuu}>
                    {dangLuu ? 'Đang lưu...' : 'Lưu học phần'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}

export default DanhSachHocPhan
