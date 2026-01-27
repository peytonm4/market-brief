import { useParams, Link, useNavigate } from 'react-router-dom'
import { useBrief, useDeleteBrief } from '../hooks/useBriefs'
import BriefViewer from '../components/briefs/BriefViewer'
import LoadingSpinner from '../components/common/LoadingSpinner'

export default function BriefDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: brief, isLoading, error } = useBrief(id)
  const deleteBrief = useDeleteBrief()

  const handleDelete = () => {
    if (window.confirm('Are you sure you want to delete this brief?')) {
      deleteBrief.mutate(id!, {
        onSuccess: () => navigate('/history'),
      })
    }
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <LoadingSpinner size="lg" />
      </div>
    )
  }

  if (error || !brief) {
    return (
      <div className="text-center py-12">
        <h2 className="text-xl font-semibold text-gray-900 mb-2">Brief Not Found</h2>
        <p className="text-gray-500 mb-4">
          The brief you're looking for doesn't exist or has been deleted.
        </p>
        <Link to="/" className="btn btn-primary">
          Back to Dashboard
        </Link>
      </div>
    )
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    })
  }

  const statusColors: Record<string, string> = {
    Draft: 'bg-gray-100 text-gray-800',
    Generating: 'bg-yellow-100 text-yellow-800',
    Completed: 'bg-green-100 text-green-800',
    Failed: 'bg-red-100 text-red-800',
    Published: 'bg-blue-100 text-blue-800',
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center space-x-2 text-sm text-gray-500 mb-2">
            <Link to="/" className="hover:text-gray-700">
              Dashboard
            </Link>
            <span>/</span>
            <span>Brief Details</span>
          </div>
          <h1 className="text-2xl font-bold text-gray-900">{brief.title}</h1>
          <div className="mt-2 flex items-center space-x-4">
            <span className="text-gray-500">{formatDate(brief.briefDate)}</span>
            <span
              className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                statusColors[brief.status] || 'bg-gray-100 text-gray-800'
              }`}
            >
              {brief.status}
            </span>
            {brief.version > 1 && (
              <span className="text-xs text-gray-400">Version {brief.version}</span>
            )}
          </div>
        </div>
        <button
          onClick={handleDelete}
          disabled={deleteBrief.isPending}
          className="btn text-red-600 hover:text-red-700 hover:bg-red-50"
        >
          {deleteBrief.isPending ? 'Deleting...' : 'Delete'}
        </button>
      </div>

      {/* Summary */}
      {brief.summary && (
        <div className="card p-6">
          <h2 className="text-sm font-medium text-gray-500 uppercase tracking-wide mb-2">
            Summary
          </h2>
          <p className="text-gray-700">{brief.summary}</p>
        </div>
      )}

      {/* Metadata */}
      <div className="card p-6">
        <h2 className="text-sm font-medium text-gray-500 uppercase tracking-wide mb-4">
          Details
        </h2>
        <dl className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
          <div>
            <dt className="text-gray-500">Created</dt>
            <dd className="mt-1 text-gray-900">
              {new Date(brief.createdAt).toLocaleString()}
            </dd>
          </div>
          <div>
            <dt className="text-gray-500">Updated</dt>
            <dd className="mt-1 text-gray-900">
              {new Date(brief.updatedAt).toLocaleString()}
            </dd>
          </div>
          {brief.generationDurationMs && (
            <div>
              <dt className="text-gray-500">Generation Time</dt>
              <dd className="mt-1 text-gray-900">
                {(brief.generationDurationMs / 1000).toFixed(2)}s
              </dd>
            </div>
          )}
          {brief.pdfGeneratedAt && (
            <div>
              <dt className="text-gray-500">PDF Generated</dt>
              <dd className="mt-1 text-gray-900">
                {new Date(brief.pdfGeneratedAt).toLocaleString()}
              </dd>
            </div>
          )}
        </dl>
      </div>

      {/* Content Viewer */}
      <BriefViewer brief={brief} />
    </div>
  )
}
