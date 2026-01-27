export interface Brief {
  id: string
  briefDate: string
  title: string
  summary: string | null
  status: BriefStatus
  hasPdf: boolean
  createdAt: string
  updatedAt: string
  publishedAt: string | null
}

export interface BriefDetail extends Brief {
  contentMarkdown: string | null
  contentJson: object | null
  pdfStoragePath: string | null
  pdfGeneratedAt: string | null
  generationStartedAt: string | null
  generationCompletedAt: string | null
  generationDurationMs: number | null
  version: number
  sections: BriefSection[]
}

export interface BriefSection {
  id: string
  sectionType: string
  title: string
  contentMarkdown: string | null
  contentJson: object | null
  displayOrder: number
}

export type BriefStatus = 'Draft' | 'Generating' | 'Completed' | 'Failed' | 'Published'

export interface PaginatedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}
