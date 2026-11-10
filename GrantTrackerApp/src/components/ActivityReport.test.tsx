import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ActivityReport } from './ActivityReport.tsx'

function report(days: number) {
  return {
    from: '2026-03-13',
    to: '2026-03-15',
    days: [
      { date: '2026-03-13', created: 2, completed: 0 },
      { date: '2026-03-14', created: 0, completed: 1 },
      { date: '2026-03-15', created: 1, completed: 3 },
    ].slice(-days),
    overdueList: { totalTasks: 120, overdueTasks: 4, queryMilliseconds: 1.25 },
  }
}

function jsonResponse(body: unknown) {
  return new Response(JSON.stringify(body), { status: 200, headers: { 'content-type': 'application/json' } })
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('ActivityReport', () => {
  it('loads nothing until opened, then shows each day newest first with totals', async () => {
    const fetchMock = vi.fn(() => Promise.resolve(jsonResponse(report(3))))
    vi.stubGlobal('fetch', fetchMock)

    render(<ActivityReport />)
    expect(fetchMock).not.toHaveBeenCalled()
    await userEvent.click(screen.getByRole('button', { name: 'Show' }))

    expect(fetchMock).toHaveBeenCalledWith('/reports/task-activity?days=14', expect.anything())
    const rows = within(await screen.findByRole('table')).getAllByRole('row')
    expect(rows.map((row) => row.textContent)).toEqual([
      'DateCreatedCompleted',
      '15/03/202613',
      '14/03/202601',
      '13/03/202620',
      'Total34',
    ])
    expect(screen.getByText(/4 overdue of 120 tasks, loaded in 1.25 ms/)).toBeInTheDocument()
  })

  it('asks for the chosen period', async () => {
    const fetchMock = vi.fn(() => Promise.resolve(jsonResponse(report(3))))
    vi.stubGlobal('fetch', fetchMock)

    render(<ActivityReport />)
    await userEvent.click(screen.getByRole('button', { name: 'Show' }))
    await userEvent.selectOptions(await screen.findByLabelText('Period'), '30')

    expect(fetchMock).toHaveBeenLastCalledWith('/reports/task-activity?days=30', expect.anything())
  })

  it('says so when the server cannot be reached', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.reject(new TypeError('Failed to fetch'))))

    render(<ActivityReport />)
    await userEvent.click(screen.getByRole('button', { name: 'Show' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('The server cannot be reached.')
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })
})
