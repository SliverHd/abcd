import axiosClient from './axiosClient'
import type { HocPhan, TaoHocPhanDto } from '../types/hocPhan'

export const hocPhanApi = {
  getAll: () => axiosClient.get<HocPhan[]>('/hoc-phan'),
  getById: (id: number) => axiosClient.get<HocPhan>(`/hoc-phan/${id}`),
  create: (data: TaoHocPhanDto) => axiosClient.post<HocPhan>('/hoc-phan', data),
  update: (id: number, data: TaoHocPhanDto) => axiosClient.put(`/hoc-phan/${id}`, data),
  delete: (id: number) => axiosClient.delete(`/hoc-phan/${id}`),
}
