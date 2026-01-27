import { useState } from 'react'
import { useBriefs } from '../hooks/useBriefs'
import { useGenerationHistory } from '../hooks/useGeneration'
import BriefCard from '../components/briefs/BriefCard'
import LoadingSpinner from '../components/common/LoadingSpinner'

type Tab = 'briefs' | 'generation'

export default function HistoryPage() {
  const [activeTab, setActiveTab] = useState<Tab>('briefs')
  const [briefsPage, setBriefsPage] = useState(1)
  const [historyPage, setHistoryPage] = useState(1)

  const { data: briefs, isLoading: isLoadingBriefs } = useBriefs(briefsPage, 10)
  const { data: history, isLoading: isLoadingHistory } = useGenerationHistory(historyPage, 20)

  const tabs = [
    { id: 'briefs' as Tab, label: 'All Briefs' },
    { id: 'generation' as Tab, label: 'Generation History' },
  ]

  const statusColors: Record<string, string> = {
    Pending: 'bg-gray-100 text-gray-800',
    Running: 'bg-yellow-100 text-yellow-800',
    Completed: 'bg-green-100 text-green-800',
    Failed: 'bg-red-100 text-red-800',
    Cancelled: 'bg-gray-100 text-gray-800',
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">History</h1>
        <p className="mt-1 text-sm text-gray-500">
          Browse past briefs and generation logs
        </p>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200">
        <nav className="-mb-px flex space-x-8">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`py-4 px-1 border-b-2 font-medium text-sm ${
                activeTab === tab.id
                  ? 'border-primary-500 text-primary-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </nav>
      </div>

      {/* Briefs Tab */}
      {activeTab === 'briefs' && (
        <div>
          {isLoadingBriefs ? (
            <div className="flex justify-center py-12">
              <LoadingSpinner />
            </div>
          ) : briefs?.items.length === 0 ? (
            <div className="text-center py-12 text-gray-500">
              No briefs found.
            </div>
          ) : (
            <>
              <div className="space-y-4">
                {briefs?.items.map((brief) => (
                  <BriefCard key={brief.id} brief={brief} />
                ))}
              </div>

              {/* Pagination */}
              {briefs && briefs.totalPages > 1 && (
                <div className="mt-6 flex items-center justify-between">
                  <p className="text-sm text-gray-500">
                    Showing {(briefsPage - 1) * 10 + 1} to{' '}
                    {Math.min(briefsPage * 10, briefs.totalCount)} of {briefs.totalCount} briefs
                  </p>
                  <div className="flex space-x-2">
                    <button
                      onClick={() => setBriefsPage((p) => Math.max(1, p - 1))}
                      disabled={briefsPage === 1}
                      className="btn btn-secondary text-sm disabled:opacity-50"
                    >
                      Previous
                    </button>
                    <button
                      onClick={() => setBriefsPage((p) => Math.min(briefs.totalPages, p + 1))}
                      disabled={briefsPage === briefs.totalPages}
                      className="btn btn-secondary text-sm disabled:opacity-50"
                    >
                      Next
                    </button>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      )}

      {/* Generation History Tab */}
      {activeTab === 'generation' && (
        <div>
          {isLoadingHistory ? (
            <div className="flex justify-center py-12">
              <LoadingSpinner />
            </div>
          ) : history?.items.length === 0 ? (
            <div className="text-center py-12 text-gray-500">
              No generation history found.
            </div>
          ) : (
            <>
              <div className="card overflow-hidden">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Started At
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Trigger
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Status
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Duration
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Error
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {history?.items.map((log) => (
                      <tr key={log.id}>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                          {new Date(log.startedAt).toLocaleString()}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {log.triggerType}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <span
                            className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                              statusColors[log.status] || 'bg-gray-100 text-gray-800'
                            }`}
                          >
                            {log.status}
                          </span>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {log.completedAt
                            ? `${((new Date(log.completedAt).getTime() - new Date(log.startedAt).getTime()) / 1000).toFixed(1)}s`
                            : '-'}
                        </td>
                        <td className="px-6 py-4 text-sm text-red-600 max-w-xs truncate">
                          {log.errorMessage || '-'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Pagination */}
              {history && history.totalPages > 1 && (
                <div className="mt-6 flex items-center justify-between">
                  <p className="text-sm text-gray-500">
                    Showing {(historyPage - 1) * 20 + 1} to{' '}
                    {Math.min(historyPage * 20, history.totalCount)} of {history.totalCount} logs
                  </p>
                  <div className="flex space-x-2">
                    <button
                      onClick={() => setHistoryPage((p) => Math.max(1, p - 1))}
                      disabled={historyPage === 1}
                      className="btn btn-secondary text-sm disabled:opacity-50"
                    >
                      Previous
                    </button>
                    <button
                      onClick={() => setHistoryPage((p) => Math.min(history.totalPages, p + 1))}
                      disabled={historyPage === history.totalPages}
                      className="btn btn-secondary text-sm disabled:opacity-50"
                    >
                      Next
                    </button>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      )}
    </div>
  )
}
