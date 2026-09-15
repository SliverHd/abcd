import axiosClient from './axiosClient'
import type {
  LichBan,
  NguyenVong,
  ThemNguyenVongRequest,
  LuuMaTranNguyenVongRequest,
} from '../types/nguyenVong'

export const nguyenVongApi = {
  getNguyenVong: (maHocKy: number, maGiangVien?: number) =>
    axiosClient.get<NguyenVong[]>('/nguyen-vong', { params: { maHocKy, maGiangVien } }),

  themNguyenVong: (data: ThemNguyenVongRequest) =>
    axiosClient.post<NguyenVong>('/nguyen-vong', data),

  xoaNguyenVong: (id: number) =>
    axiosClient.delete<{ message: string }>(`/nguyen-vong/${id}`),

  getLichBan: (maHocKy: number, maGiangVien?: number) =>
    axiosClient.get<LichBan[]>('/nguyen-vong/lich-ban', { params: { maHocKy, maGiangVien } }),

  luuMaTran: (data: LuuMaTranNguyenVongRequest) =>
    axiosClient.post<{ message: string }>('/nguyen-vong/luu-ma-tran', data),
}

