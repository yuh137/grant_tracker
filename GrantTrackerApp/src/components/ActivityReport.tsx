import { useCallback, useEffect, useId, useState } from 'react'
import { api, ConnectionError, formatDate, type TaskActivityReport } from '../api.ts'

const PERIODS = [7, 14, 30, 90]

export function ActivityReport() {
  const [open, setOpen] = useState(false)
  const [days, setDays] = useState(14)
  const [report, setReport] = useState<TaskActivityReport | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const bodyId = useId()

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setReport(await api.taskActivity(days))
    } catch (failure) {
      setError(
        failure instanceof ConnectionError ? failure.message : 'The report could not be loaded. Please try again.',
      )
    } finally {
      setLoading(false)
    }
  }, [days])

  useEffect(() => {
    if (open) {
      void load()
    }
  }, [open, load])

  const newestFirst = report ? [...report.days].reverse() : []
  const totalCreated = newestFirst.reduce((sum, day) => sum + day.created, 0)
  const totalCompleted = newestFirst.reduce((sum, day) => sum + day.completed, 0)

  return (
    <section className="report" aria-label="Task activity">
      <div className="report-header">
        <h3>Task activity</h3>
        <button type="button" aria-expanded={open} aria-controls={bodyId} onClick={() => setOpen((value) => !value)}>
          {open ? 'Hide' : 'Show'}
        </button>
      </div>

      {open && (
        <div id={bodyId}>
          <div className="report-toolbar">
            <label>
              Period
              <select value={days} onChange={(event) => setDays(Number(event.target.value))}>
                {PERIODS.map((period) => (
                  <option key={period} value={period}>
                    Last {period} days
                  </option>
                ))}
              </select>
            </label>
            <button type="button" onClick={() => void load()} disabled={loading}>
              {loading ? 'Loading…' : 'Refresh'}
            </button>
          </div>

          {error && (
            <p className="error" role="alert">
              {error}
            </p>
          )}

          {report && (
            <>
              <p className="meta">
                Overdue list: {report.overdueList.overdueTasks} overdue of {report.overdueList.totalTasks} tasks,
                loaded in {report.overdueList.queryMilliseconds} ms. Days follow the office's time zone.
              </p>
              <table className="activity">
                <thead>
                  <tr>
                    <th>Date</th>
                    <th>Created</th>
                    <th>Completed</th>
                  </tr>
                </thead>
                <tbody>
                  {newestFirst.map((day) => (
                    <tr key={day.date}>
                      <td>{formatDate(day.date)}</td>
                      <td>{day.created}</td>
                      <td>{day.completed}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr>
                    <th>Total</th>
                    <td>{totalCreated}</td>
                    <td>{totalCompleted}</td>
                  </tr>
                </tfoot>
              </table>
            </>
          )}
        </div>
      )}
    </section>
  )
}
