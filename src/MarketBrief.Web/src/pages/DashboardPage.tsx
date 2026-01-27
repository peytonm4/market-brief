import { useEffect, useRef } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { useLatestBrief, useBriefs } from '../hooks/useBriefs'
import { useGenerationStatus } from '../hooks/useGeneration'
import BriefCard from '../components/briefs/BriefCard'
import BriefViewer from '../components/briefs/BriefViewer'
import GenerationTrigger from '../components/generation/GenerationTrigger'
import GenerationStatus from '../components/generation/GenerationStatus'
import LoadingSpinner from '../components/common/LoadingSpinner'

export default function DashboardPage() {
  const queryClient = useQueryClient()
  const { data: latestBrief, isLoading: isLoadingLatest, error: latestError } = useLatestBrief()
  const { data: recentBriefs, isLoading: isLoadingRecent } = useBriefs(1, 5)
  const { data: generationStatus } = useGenerationStatus()

  const isGenerating = generationStatus?.isRunning ?? false
  const wasGeneratingRef = useRef(false)

  // Refetch briefs when generation completes
  useEffect(() => {
    if (wasGeneratingRef.current && !isGenerating) {
      // Generation just finished - refetch all briefs data
      queryClient.invalidateQueries({ queryKey: ['briefs'] })
    }
    wasGeneratingRef.current = isGenerating
  }, [isGenerating, queryClient])

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
          <p className="mt-1 text-sm text-gray-500">
            View and generate daily market briefs
          </p>
        </div>
        <GenerationTrigger disabled={isGenerating} />
      </div>

      {/* Generation Status */}
      {isGenerating && <GenerationStatus />}

      {/* Latest Brief */}
      <section>
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Latest Brief</h2>
        {isLoadingLatest ? (
          <div className="card p-8">
            <LoadingSpinner />
          </div>
        ) : latestError ? (
          <div className="card p-8 text-center">
            <p className="text-gray-500">No briefs available yet.</p>
            <p className="text-sm text-gray-400 mt-1">
              Click "Generate Brief" to create your first market brief.
            </p>
          </div>
        ) : latestBrief ? (
          <BriefViewer brief={latestBrief} />
        ) : null}
      </section>

      {/* Recent Briefs */}
      <section>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">Recent Briefs</h2>
          <a
            href="/history"
            className="text-sm font-medium text-primary-600 hover:text-primary-700"
          >
            View all
          </a>
        </div>
        {isLoadingRecent ? (
          <div className="card p-8">
            <LoadingSpinner />
          </div>
        ) : recentBriefs?.items.length === 0 ? (
          <div className="card p-8 text-center text-gray-500">
            No briefs found.
          </div>
        ) : (
          <div className="card divide-y divide-gray-100">
            {recentBriefs?.items.map((brief) => (
              <BriefCard key={brief.id} brief={brief} compact />
            ))}
          </div>
        )}
      </section>
    </div>
  )
}
