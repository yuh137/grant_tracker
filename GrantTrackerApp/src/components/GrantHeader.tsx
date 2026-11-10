import { formatDate, type GrantDetail } from '../api.ts'

interface Props {
  grant: GrantDetail
  closing: boolean
  closeError: string | null
  onClose: () => void
}

export function GrantHeader({ grant, closing, closeError, onClose }: Props) {
  const pending = grant.tasks.filter((t) => !t.isCompleted).length
  const overdue = grant.tasks.filter((t) => t.isOverdue).length

  return (
    <section className="grant-header">
      <div>
        <h2>
          {grant.organisationName} <span className="reference">{grant.reference}</span>
        </h2>
        <p className="meta">
          Applied {formatDate(grant.applicationDate)} · {pending} pending
          {overdue > 0 && <strong className="overdue-count"> · {overdue} overdue</strong>}
        </p>
      </div>
      <div className="grant-actions">
        <span className={`status status-${grant.status.toLowerCase()}`}>{grant.status}</span>
        {grant.status === 'Open' && (
          <button type="button" onClick={onClose} disabled={closing}>
            {closing ? 'Closing…' : 'Close grant'}
          </button>
        )}
      </div>
      {closeError && (
        <p className="error" role="alert">
          {closeError}
        </p>
      )}
    </section>
  )
}
