import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from './App.tsx'

const grant = {
  id: 'g1',
  reference: 'GR-100001',
  organisationName: 'Riverside Community Garden Inc.',
  applicationDate: '2026-01-14',
  status: 'Open',
  pendingTaskCount: 2,
}

const detail = {
  ...grant,
  tasks: [
    {
      id: 't1',
      grantId: 'g1',
      title: 'Verify bank details',
      priority: 'High',
      dueDate: '2026-03-12',
      createdOn: '2026-01-14T00:00:00+11:00',
      completedOn: null,
      isCompleted: false,
      isOverdue: true,
    },
    {
      id: 't2',
      grantId: 'g1',
      title: 'Request quote for tools',
      priority: 'Medium',
      dueDate: '2026-03-20',
      createdOn: '2026-01-14T00:00:00+11:00',
      completedOn: null,
      isCompleted: false,
      isOverdue: false,
    },
  ],
}

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } })
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('App', () => {
  it('shows the connection banner when the API cannot be reached and retries', async () => {
    const fetchMock = vi
      .fn()
      .mockRejectedValueOnce(new TypeError('Failed to fetch'))
      .mockResolvedValue(jsonResponse([grant]))
    vi.stubGlobal('fetch', fetchMock)

    render(<App />)

    expect(await screen.findByRole('alert')).toHaveTextContent('The server cannot be reached.')
    fetchMock.mockImplementation((url: string) =>
      Promise.resolve(url.startsWith('/grants/g1') ? jsonResponse(detail) : jsonResponse([grant])),
    )
    await userEvent.click(screen.getByRole('button', { name: 'Retry' }))
    await waitFor(() => expect(screen.queryByRole('alert')).not.toBeInTheDocument())
  })

  it('lists the selected grant tasks and marks overdue ones', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn((url: string) =>
        Promise.resolve(url.startsWith('/grants/g1') ? jsonResponse(detail) : jsonResponse([grant])),
      ),
    )

    render(<App />)

    const overdueRow = (await screen.findByText('Verify bank details')).closest('tr')
    expect(overdueRow).toHaveClass('overdue')
    expect(overdueRow).toHaveTextContent('Overdue')
    expect(screen.getByText('Request quote for tools').closest('tr')).not.toHaveClass('overdue')
    expect(screen.getAllByRole('button', { name: 'Complete' })).toHaveLength(2)
  })

  it('shows the reason when closing is refused', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn((url: string, init?: RequestInit) => {
        if (init?.method === 'PUT') {
          return Promise.resolve(
            jsonResponse(
              {
                status: 422,
                title: 'Business rule violated',
                detail: 'A grant cannot be closed while it has a pending High-priority task.',
                code: 'PendingHighPriorityTask',
              },
              422,
            ),
          )
        }
        return Promise.resolve(url.startsWith('/grants/g1') ? jsonResponse(detail) : jsonResponse([grant]))
      }),
    )

    render(<App />)
    await userEvent.click(await screen.findByRole('button', { name: 'Close grant' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('pending High-priority task. (PendingHighPriorityTask)')
  })
})
