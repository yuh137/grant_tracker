import { useState, type FormEvent } from 'react'
import { ApiError, ConnectionError, todayIso, type NewTask, type TaskPriority } from '../api.ts'

export const MaxTitleLength = 100

interface Props {
  grantClosed: boolean
  applicationDate: string
  onSubmit: (task: NewTask) => Promise<void>
  onConnectionLost: () => void
}

interface FieldErrors {
  title?: string
  priority?: string
  dueDate?: string
}

export function validate(title: string, priority: string, dueDate: string, applicationDate: string): FieldErrors {
  const errors: FieldErrors = {}
  const trimmed = title.trim()
  if (!trimmed) {
    errors.title = 'Title is required.'
  } else if (trimmed.length > MaxTitleLength) {
    errors.title = `Title must be ${MaxTitleLength} characters or fewer.`
  }
  if (!priority) {
    errors.priority = 'Choose a priority.'
  }
  if (!dueDate) {
    errors.dueDate = 'Due date is required.'
  } else if (dueDate < todayIso()) {
    errors.dueDate = 'Due date cannot be in the past.'
  } else if (dueDate < applicationDate) {
    errors.dueDate = 'Due date cannot be before the application date.'
  }
  return errors
}

export function AddTaskForm({ grantClosed, applicationDate, onSubmit, onConnectionLost }: Props) {
  const [title, setTitle] = useState('')
  const [priority, setPriority] = useState<TaskPriority | ''>('')
  const [dueDate, setDueDate] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [apiError, setApiError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setApiError(null)
    const errors = validate(title, priority, dueDate, applicationDate)
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0) return

    setSubmitting(true)
    try {
      await onSubmit({ title: title.trim(), priority: priority as TaskPriority, dueDate })
      setTitle('')
      setPriority('')
      setDueDate('')
    } catch (error) {
      if (error instanceof ConnectionError) {
        setApiError(error.message)
        onConnectionLost()
      } else if (error instanceof ApiError) {
        setApiError(describeProblem(error))
      } else {
        setApiError('Something went wrong. Please try again.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  if (grantClosed) {
    return <p className="empty">This grant is closed. Tasks can no longer be added.</p>
  }

  return (
    <form className="add-task" onSubmit={(e) => void handleSubmit(e)} noValidate>
      <h3>Add task</h3>
      <div className="fields">
        <div className="field">
          <label htmlFor="task-title">Title</label>
          <input
            id="task-title"
            value={title}
            maxLength={MaxTitleLength + 20}
            onChange={(e) => setTitle(e.target.value)}
            aria-invalid={Boolean(fieldErrors.title)}
            placeholder="e.g. Verify bank details"
          />
          {fieldErrors.title && <span className="field-error">{fieldErrors.title}</span>}
          <span className="hint">
            {title.trim().length}/{MaxTitleLength}
          </span>
        </div>
        <div className="field">
          <label htmlFor="task-priority">Priority</label>
          <select
            id="task-priority"
            value={priority}
            onChange={(e) => setPriority(e.target.value as TaskPriority | '')}
            aria-invalid={Boolean(fieldErrors.priority)}
          >
            <option value="">Choose…</option>
            <option value="Low">Low</option>
            <option value="Medium">Medium</option>
            <option value="High">High</option>
          </select>
          {fieldErrors.priority && <span className="field-error">{fieldErrors.priority}</span>}
        </div>
        <div className="field">
          <label htmlFor="task-due">Due date</label>
          <input
            id="task-due"
            type="date"
            value={dueDate}
            min={todayIso() > applicationDate ? todayIso() : applicationDate}
            onChange={(e) => setDueDate(e.target.value)}
            aria-invalid={Boolean(fieldErrors.dueDate)}
          />
          {fieldErrors.dueDate && <span className="field-error">{fieldErrors.dueDate}</span>}
        </div>
      </div>
      {apiError && (
        <p className="error" role="alert">
          {apiError}
        </p>
      )}
      <button type="submit" disabled={submitting}>
        {submitting ? 'Adding…' : 'Add task'}
      </button>
    </form>
  )
}

function describeProblem(error: ApiError): string {
  const { problem } = error
  if (problem.errors) {
    return Object.values(problem.errors).flat().join(' ')
  }
  if (problem.status === 409) {
    return 'Another officer changed this grant a moment ago. Reload and try again.'
  }
  const code = problem.code ? ` (${problem.code})` : ''
  return `${problem.detail ?? problem.title}${code}`
}
