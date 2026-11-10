import { useCallback, useEffect, useState } from 'react'
import { api, ApiError, ConnectionError, type GrantDetail, type GrantStatus, type GrantSummary, type NewTask } from './api.ts'
import { ActivityReport } from './components/ActivityReport.tsx'
import { AddTaskForm } from './components/AddTaskForm.tsx'
import { ConnectionBanner } from './components/ConnectionBanner.tsx'
import { GrantHeader } from './components/GrantHeader.tsx'
import { GrantPicker, type StatusFilter } from './components/GrantPicker.tsx'
import { TaskList } from './components/TaskList.tsx'

export default function App() {
  const [filter, setFilter] = useState<StatusFilter>('Open')
  const [grants, setGrants] = useState<GrantSummary[]>([])
  const [selectedId, setSelectedId] = useState<string>('')
  const [grant, setGrant] = useState<GrantDetail | null>(null)
  const [connectionLost, setConnectionLost] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)
  const [busyTaskId, setBusyTaskId] = useState<string | null>(null)
  const [closeError, setCloseError] = useState<string | null>(null)
  const [closing, setClosing] = useState(false)

  const loadGrants = useCallback(async () => {
    const status: GrantStatus | undefined = filter === 'All' ? undefined : filter
    const list = await api.listGrants(status)
    setGrants(list)
    setSelectedId((current) => (list.some((g) => g.id === current) ? current : (list[0]?.id ?? '')))
  }, [filter])

  const loadGrant = useCallback(async () => {
    if (!selectedId) {
      setGrant(null)
      return
    }
    setGrant(await api.getGrant(selectedId))
  }, [selectedId])

  const guard = useCallback(async (work: () => Promise<void>) => {
    try {
      await work()
      setConnectionLost(false)
    } catch (error) {
      if (error instanceof ConnectionError) {
        setConnectionLost(true)
        return
      }
      throw error
    }
  }, [])

  const refresh = useCallback(async () => {
    await guard(async () => {
      await loadGrants()
      await loadGrant()
    })
  }, [guard, loadGrants, loadGrant])

  useEffect(() => {
    void guard(loadGrants)
  }, [guard, loadGrants])

  useEffect(() => {
    void guard(loadGrant)
  }, [guard, loadGrant])

  async function handleAddTask(task: NewTask) {
    if (!grant) return
    await api.addTask(grant.id, task)
    setNotice(`Task "${task.title}" added.`)
    await refresh()
  }

  async function handleComplete(taskId: string) {
    setBusyTaskId(taskId)
    setNotice(null)
    try {
      await guard(async () => {
        await api.completeTask(taskId)
        await loadGrants()
        await loadGrant()
      })
    } catch (error) {
      setNotice(describe(error))
      await refresh()
    } finally {
      setBusyTaskId(null)
    }
  }

  async function handleClose() {
    if (!grant) return
    setClosing(true)
    setCloseError(null)
    try {
      await guard(async () => {
        await api.closeGrant(grant.id)
        setNotice(`Grant ${grant.reference} closed.`)
        await refresh()
      })
    } catch (error) {
      setCloseError(describe(error))
      if (error instanceof ApiError && error.problem.status === 409) {
        await refresh()
      }
    } finally {
      setClosing(false)
    }
  }

  return (
    <main className="page">
      <header className="page-header">
        <h1>Grant Task Tracker</h1>
        <GrantPicker
          filter={filter}
          grants={grants}
          selectedId={selectedId}
          onFilterChange={(value) => {
            setFilter(value)
            setNotice(null)
          }}
          onSelect={(id) => {
            setSelectedId(id)
            setNotice(null)
            setCloseError(null)
          }}
        />
      </header>

      {connectionLost && <ConnectionBanner onRetry={() => void refresh()} />}
      {notice && (
        <p className="notice" role="status">
          {notice}
        </p>
      )}

      {grant ? (
        <>
          <GrantHeader grant={grant} closing={closing} closeError={closeError} onClose={() => void handleClose()} />
          <TaskList tasks={grant.tasks} busyTaskId={busyTaskId} onComplete={(id) => void handleComplete(id)} />
          <AddTaskForm
            key={grant.id}
            grantClosed={grant.status === 'Closed'}
            applicationDate={grant.applicationDate}
            onSubmit={handleAddTask}
            onConnectionLost={() => setConnectionLost(true)}
          />
        </>
      ) : (
        !connectionLost && <p className="empty">No grants match this filter.</p>
      )}

      <ActivityReport />
    </main>
  )
}

export function describe(error: unknown): string {
  if (error instanceof ApiError) {
    const { problem } = error
    if (problem.status === 409) {
      return 'Another officer changed this grant a moment ago. The latest version has been reloaded.'
    }
    if (problem.errors) {
      return Object.values(problem.errors).flat().join(' ')
    }
    const code = problem.code ? ` (${problem.code})` : ''
    return `${problem.detail ?? problem.title}${code}`
  }
  if (error instanceof ConnectionError) {
    return error.message
  }
  return 'Something went wrong. Please try again.'
}
