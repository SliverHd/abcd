export interface NangLucGiangDay {
  maNangLuc: number
  maGiangVien: number
  maHocPhan: number
  tenHocPhan: string
  soTinChi: number
  mucDoChuyenMon: number
  trangThai: string
}

export interface GiangVien {
  maGiangVien: number
  hoTen: string
  email: string
  soDienThoai?: string
  chucDanh?: string
  maBoMon?: number
  tenBoMon?: string
  trangThai: string
  danhSachNangLuc?: NangLucGiangDay[]
}

export interface TaoGiangVienDto {
  hoTen: string
  email: string
  soDienThoai?: string
  chucDanh?: string
  maBoMon?: number
  trangThai: string
  danhSachMaHocPhan?: number[]
}
