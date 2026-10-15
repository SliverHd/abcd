export interface LichBan {
  maLichBan: number
  maGiangVien: number
  maHocKy: number
  thu: number
  tietBatDau: number
  tietKetThuc: number
  lyDo?: string
}

export interface TaoLichBanDto {
  thu: number
  tietBatDau: number
  tietKetThuc: number
  lyDo?: string
}

export interface NguyenVong {
  maNguyenVong: number
  maGiangVien: number
  maHocKy: number
  loaiNguyenVong: 'ThichDay' | 'TranhDay' | string
  mucDo: number
  thu?: number
  tietBatDau?: number
  tietKetThuc?: number
  trongSoPhat: number
  noiDungGoc?: string
}

export interface TaoNguyenVongDto {
  loaiNguyenVong: 'ThichDay' | 'TranhDay' | string
  mucDo: number
  thu?: number
  tietBatDau?: number
  tietKetThuc?: number
  ghiChu?: string
}

export interface LuuMaTranNguyenVongRequest {
  maGiangVien: number
  maHocKy: number
  danhSachLichBan: TaoLichBanDto[]
  danhSachNguyenVong: TaoNguyenVongDto[]
}

export type TrangThaiTiet = 'FREE' | 'BUSY' | 'PREFERRED' | 'AVOID'
