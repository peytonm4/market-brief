import { useEffect, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { generationApi } from '../services/generationApi'
import type { TriggerGenerationRequest } from '../types/generation'

export function useGenerationStatus(enabled = true) {
  const queryClient = useQueryClient()
  const wasRunningRef = useRef(false)

  const query = useQuery({
    queryKey: ['generation', 'status'],
    queryFn: () => generationApi.getStatus(),
    refetchInterval: (query) => {
      // Poll every 2 seconds while generation is running
      return query.state.data?.isRunning ? 2000 : false
    },
    enabled,
  })

  // Watch for generation completion and invalidate briefs
  useEffect(() => {
    if (query.data) {
      if (wasRunningRef.current && !query.data.isRunning) {
        // Generation just completed - refresh briefs data
        queryClient.invalidateQueries({ queryKey: ['briefs'] })
      }
      wasRunningRef.current = query.data.isRunning
    }
  }, [query.data, queryClient])

  return query
}

export function useGenerationHistory(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: ['generation', 'history', page, pageSize],
    queryFn: () => generationApi.getHistory(page, pageSize),
  })
}

export function useTriggerGeneration() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request?: TriggerGenerationRequest) => generationApi.trigger(request),
    onSuccess: () => {
      // Immediately start polling status
      queryClient.invalidateQueries({ queryKey: ['generation', 'status'] })
    },
  })
}
