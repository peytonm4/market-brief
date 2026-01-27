import { Link } from 'react-router-dom'
import type { Brief } from '../../types/brief'

interface BriefCardProps {
  brief: Brief
  compact?: boolean
}

export default function BriefCard({ brief, compact = false }: BriefCardProps) {
  const statusColors: Record<string, string> = {
    Draft: 'bg-gray-100 text-gray-800',
    Generating: 'bg-yellow-100 text-yellow-800',
    Completed: 'bg-green-100 text-green-800',
    Failed: 'bg-red-100 text-red-800',
    Published: 'bg-blue-100 text-blue-800',
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      weekday: 'short',
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    })
  }

  if (compact) {
    return (
      <Link
        to={`/briefs/${brief.id}`}
        className="block p-4 hover:bg-gray-50 transition-colors border-b border-gray-100 last:border-b-0"
      >
        <div className="flex items-center justify-between">
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-gray-900 truncate">
              {formatDate(brief.briefDate)}
            </p>
            <p className="text-sm text-gray-500 truncate">{brief.title}</p>
          </div>
          <div className="ml-4 flex items-center space-x-2">
            <span
              className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${
                statusColors[brief.status] || 'bg-gray-100 text-gray-800'
              }`}
            >
              {brief.status}
            </span>
            {brief.hasPdf && (
              <span className="text-gray-400">
                <svg className="h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
                  <path
                    fillRule="evenodd"
                    d="M4 4a2 2 0 012-2h4.586A2 2 0 0112 2.586L15.414 6A2 2 0 0116 7.414V16a2 2 0 01-2 2H6a2 2 0 01-2-2V4z"
                    clipRule="evenodd"
                  />
                </svg>
              </span>
            )}
          </div>
        </div>
      </Link>
    )
  }

  return (
    <div className="card p-6">
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="flex items-center space-x-3">
            <h3 className="text-lg font-semibold text-gray-900">{brief.title}</h3>
            <span
              className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                statusColors[brief.status] || 'bg-gray-100 text-gray-800'
              }`}
            >
              {brief.status}
            </span>
          </div>
          <p className="mt-1 text-sm text-gray-500">{formatDate(brief.briefDate)}</p>
          {brief.summary && (
            <p className="mt-3 text-sm text-gray-600 line-clamp-2">{brief.summary}</p>
          )}
        </div>
      </div>

      <div className="mt-4 flex items-center justify-between">
        <div className="flex items-center space-x-4 text-xs text-gray-500">
          <span>Created: {new Date(brief.createdAt).toLocaleString()}</span>
          {brief.publishedAt && (
            <span>Published: {new Date(brief.publishedAt).toLocaleString()}</span>
          )}
        </div>
        <Link
          to={`/briefs/${brief.id}`}
          className="text-sm font-medium text-primary-600 hover:text-primary-700"
        >
          View Details
        </Link>
      </div>
    </div>
  )
}
