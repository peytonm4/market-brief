import api from './api'
import type { Brief, BriefDetail, PaginatedResponse } from '../types/brief'

export const briefsApi = {
  getAll: async (page = 1, pageSize = 10): Promise<PaginatedResponse<Brief>> => {
    const response = await api.get('/briefs', {
      params: { page, pageSize },
    })
    return response.data
  },

  getLatest: async (): Promise<BriefDetail> => {
    const response = await api.get('/briefs/latest')
    return response.data
  },

  getById: async (id: string): Promise<BriefDetail> => {
    const response = await api.get(`/briefs/${id}`)
    return response.data
  },

  getByDate: async (date: string): Promise<BriefDetail> => {
    const response = await api.get(`/briefs/date/${date}`)
    return response.data
  },

  getAsMarkdown: async (id: string): Promise<string> => {
    const response = await api.get(`/briefs/${id}/markdown`)
    return response.data
  },

  getAsJson: async (id: string): Promise<object> => {
    const response = await api.get(`/briefs/${id}/json`)
    return response.data
  },

  downloadPdf: async (id: string): Promise<Blob> => {
    const response = await api.get(`/briefs/${id}/pdf`, {
      responseType: 'blob',
    })
    return response.data
  },

  regeneratePdf: async (id: string): Promise<{ message: string; path: string }> => {
    const response = await api.post(`/briefs/${id}/pdf/regenerate`)
    return response.data
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/briefs/${id}`)
  },
}
