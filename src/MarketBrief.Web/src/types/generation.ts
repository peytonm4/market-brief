export interface GenerationStatus {
  isRunning: boolean
  currentJobId: string | null
  status: string | null
  startedAt: string | null
  briefId: string | null
  message: string | null
}

export interface GenerationHistory {
  id: string
  briefId: string | null
  jobId: string | null
  triggerType: 'Scheduled' | 'Manual' | 'Retry'
  status: 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled'
  startedAt: string
  completedAt: string | null
  errorMessage: string | null
}

export interface TriggerGenerationRequest {
  date?: string
  force?: boolean
}

export interface TriggerGenerationResponse {
  jobId: string
  message: string
  targetDate: string
}
