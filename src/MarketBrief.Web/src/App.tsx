import { Routes, Route } from 'react-router-dom'
import MainLayout from './components/layout/MainLayout'
import DashboardPage from './pages/DashboardPage'
import BriefDetailPage from './pages/BriefDetailPage'
import HistoryPage from './pages/HistoryPage'

function App() {
  return (
    <MainLayout>
      <Routes>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/briefs/:id" element={<BriefDetailPage />} />
        <Route path="/history" element={<HistoryPage />} />
      </Routes>
    </MainLayout>
  )
}

export default App
