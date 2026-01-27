import { useGenerationStatus } from '../../hooks/useGeneration'
import LoadingSpinner from '../common/LoadingSpinner'

interface GenerationStatusProps {
  className?: string
}

export default function GenerationStatus({ className = '' }: GenerationStatusProps) {
  const { data: status, isLoading } = useGenerationStatus()

  if (isLoading) {
    return (
      <div className={`flex items-center space-x-2 ${className}`}>
        <LoadingSpinner size="sm" />
        <span className="text-sm text-gray-500">Checking status...</span>
      </div>
    )
  }

  if (!status) {
    return null
  }

  if (!status.isRunning) {
    return (
      <div className={`flex items-center space-x-2 ${className}`}>
        <span className="h-2 w-2 rounded-full bg-green-500" />
        <span className="text-sm text-gray-600">Ready for generation</span>
      </div>
    )
  }

  return (
    <div className={`flex items-center space-x-3 p-4 bg-yellow-50 rounded-lg border border-yellow-200 ${className}`}>
      <div className="flex-shrink-0">
        <svg className="animate-spin h-5 w-5 text-yellow-600" fill="none" viewBox="0 0 24 24">
          <circle
            className="opacity-25"
            cx="12"
            cy="12"
            r="10"
            stroke="currentColor"
            strokeWidth="4"
          />
          <path
            className="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
          />
        </svg>
      </div>
      <div className="flex-1">
        <p className="text-sm font-medium text-yellow-800">Generation in progress</p>
        <p className="text-xs text-yellow-600 mt-0.5">
          {status.message}
          {status.startedAt && (
            <> - Started at {new Date(status.startedAt).toLocaleTimeString()}</>
          )}
        </p>
      </div>
    </div>
  )
}
