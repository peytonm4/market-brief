import api from './api'
import type { GenerationStatus, GenerationHistory, TriggerGenerationRequest, TriggerGenerationResponse } from '../types/generation'
import type { PaginatedResponse } from '../types/brief'

export const generationApi = {
  trigger: async (request?: TriggerGenerationRequest): Promise<TriggerGenerationResponse> => {
    const response = await api.post('/generation/trigger', request || {})
    return response.data
  },

  getStatus: async (): Promise<GenerationStatus> => {
    const response = await api.get('/generation/status')
    return response.data
  },

  getHistory: async (page = 1, pageSize = 20): Promise<PaginatedResponse<GenerationHistory>> => {
    const response = await api.get('/generation/history', {
      params: { page, pageSize },
    })
    return response.data
  },
}
