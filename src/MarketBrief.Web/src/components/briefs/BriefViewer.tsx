import { useState } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import type { BriefDetail } from '../../types/brief'
import { useDownloadPdf, useRegeneratePdf } from '../../hooks/useBriefs'

interface BriefViewerProps {
  brief: BriefDetail
}

type ViewMode = 'rendered' | 'markdown' | 'json'

export default function BriefViewer({ brief }: BriefViewerProps) {
  const [viewMode, setViewMode] = useState<ViewMode>('rendered')
  const downloadPdf = useDownloadPdf()
  const regeneratePdf = useRegeneratePdf()

  const handleDownloadPdf = () => {
    downloadPdf.mutate({
      id: brief.id,
      filename: `market-brief-${brief.briefDate}.pdf`,
    })
  }

  const handleRegeneratePdf = () => {
    regeneratePdf.mutate(brief.id)
  }

  return (
    <div className="card">
      <div className="border-b border-gray-200 px-6 py-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <button
              onClick={() => setViewMode('rendered')}
              className={`px-3 py-1.5 text-sm font-medium rounded-md ${
                viewMode === 'rendered'
                  ? 'bg-primary-100 text-primary-700'
                  : 'text-gray-500 hover:text-gray-700'
              }`}
            >
              Rendered
            </button>
            <button
              onClick={() => setViewMode('markdown')}
              className={`px-3 py-1.5 text-sm font-medium rounded-md ${
                viewMode === 'markdown'
                  ? 'bg-primary-100 text-primary-700'
                  : 'text-gray-500 hover:text-gray-700'
              }`}
            >
              Markdown
            </button>
            <button
              onClick={() => setViewMode('json')}
              className={`px-3 py-1.5 text-sm font-medium rounded-md ${
                viewMode === 'json'
                  ? 'bg-primary-100 text-primary-700'
                  : 'text-gray-500 hover:text-gray-700'
              }`}
            >
              JSON
            </button>
          </div>

          <div className="flex items-center space-x-2">
            {brief.hasPdf && (
              <button
                onClick={handleDownloadPdf}
                disabled={downloadPdf.isPending}
                className="btn btn-secondary text-sm flex items-center space-x-1"
              >
                <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                  />
                </svg>
                <span>{downloadPdf.isPending ? 'Downloading...' : 'Download PDF'}</span>
              </button>
            )}
            <button
              onClick={handleRegeneratePdf}
              disabled={regeneratePdf.isPending}
              className="btn btn-secondary text-sm"
            >
              {regeneratePdf.isPending ? 'Regenerating...' : 'Regenerate PDF'}
            </button>
          </div>
        </div>
      </div>

      <div className="p-6">
        {viewMode === 'rendered' && brief.contentMarkdown && (
          <div className="markdown-content">
            <ReactMarkdown remarkPlugins={[remarkGfm]}>
              {brief.contentMarkdown}
            </ReactMarkdown>
          </div>
        )}

        {viewMode === 'markdown' && (
          <pre className="bg-gray-50 p-4 rounded-lg overflow-x-auto text-sm">
            <code>{brief.contentMarkdown || 'No markdown content available'}</code>
          </pre>
        )}

        {viewMode === 'json' && (
          <pre className="bg-gray-50 p-4 rounded-lg overflow-x-auto text-sm">
            <code>
              {brief.contentJson
                ? JSON.stringify(brief.contentJson, null, 2)
                : 'No JSON content available'}
            </code>
          </pre>
        )}

        {!brief.contentMarkdown && !brief.contentJson && (
          <div className="text-center py-8 text-gray-500">
            No content available. Try generating the brief.
          </div>
        )}
      </div>
    </div>
  )
}
