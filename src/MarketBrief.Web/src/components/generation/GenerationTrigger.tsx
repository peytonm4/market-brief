import { useState } from 'react'
import { useTriggerGeneration } from '../../hooks/useGeneration'

interface GenerationTriggerProps {
  disabled?: boolean
  onSuccess?: () => void
}

export default function GenerationTrigger({ disabled, onSuccess }: GenerationTriggerProps) {
  const [date, setDate] = useState('')
  const triggerGeneration = useTriggerGeneration()

  const handleTrigger = () => {
    triggerGeneration.mutate(
      date ? { date } : undefined,
      {
        onSuccess: () => {
          setDate('')
          onSuccess?.()
        },
      }
    )
  }

  const today = new Date().toISOString().split('T')[0]

  return (
    <div className="flex items-center space-x-3">
      <input
        type="date"
        value={date}
        onChange={(e) => setDate(e.target.value)}
        max={today}
        className="block rounded-md border-gray-300 shadow-sm focus:border-primary-500 focus:ring-primary-500 text-sm"
        placeholder="Select date (optional)"
      />
      <button
        onClick={handleTrigger}
        disabled={disabled || triggerGeneration.isPending}
        className="btn btn-primary flex items-center space-x-2"
      >
        {triggerGeneration.isPending ? (
          <>
            <svg className="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
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
            <span>Starting...</span>
          </>
        ) : (
          <>
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M13 10V3L4 14h7v7l9-11h-7z"
              />
            </svg>
            <span>Generate Brief</span>
          </>
        )}
      </button>

      {triggerGeneration.isError && (
        <span className="text-sm text-red-600">
          {(triggerGeneration.error as Error).message || 'Failed to trigger generation'}
        </span>
      )}
    </div>
  )
}
