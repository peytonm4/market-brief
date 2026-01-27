import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { briefsApi } from '../services/briefsApi'

export function useBriefs(page = 1, pageSize = 10) {
  return useQuery({
    queryKey: ['briefs', page, pageSize],
    queryFn: () => briefsApi.getAll(page, pageSize),
  })
}

export function useLatestBrief() {
  return useQuery({
    queryKey: ['briefs', 'latest'],
    queryFn: () => briefsApi.getLatest(),
    retry: false,
  })
}

export function useBrief(id: string | undefined) {
  return useQuery({
    queryKey: ['briefs', id],
    queryFn: () => briefsApi.getById(id!),
    enabled: !!id,
  })
}

export function useBriefByDate(date: string | undefined) {
  return useQuery({
    queryKey: ['briefs', 'date', date],
    queryFn: () => briefsApi.getByDate(date!),
    enabled: !!date,
  })
}

export function useDownloadPdf() {
  return useMutation({
    mutationFn: async ({ id, filename }: { id: string; filename: string }) => {
      const blob = await briefsApi.downloadPdf(id)
      const url = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = filename
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      window.URL.revokeObjectURL(url)
    },
  })
}

export function useRegeneratePdf() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => briefsApi.regeneratePdf(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ['briefs', id] })
      queryClient.invalidateQueries({ queryKey: ['briefs', 'latest'] })
    },
  })
}

export function useDeleteBrief() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => briefsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['briefs'] })
    },
  })
}
