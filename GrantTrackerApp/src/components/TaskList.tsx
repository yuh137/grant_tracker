import { formatDate, type TaskItem } from '../api.ts'

interface Props {
  tasks: TaskItem[]
  busyTaskId: string | null
  onComplete: (taskId: string) => void
}

export function TaskList({ tasks, busyTaskId, onComplete }: Props) {
  if (tasks.length === 0) {
    return <p className="empty">No tasks yet.</p>
  }

  return (
    <table className="tasks">
      <thead>
        <tr>
          <th>Task</th>
          <th>Priority</th>
          <th>Due</th>
          <th>Status</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        {tasks.map((task) => (
          <tr key={task.id} className={task.isOverdue ? 'overdue' : task.isCompleted ? 'completed' : ''}>
            <td>{task.title}</td>
            <td>
              <span className={`priority priority-${task.priority.toLowerCase()}`}>{task.priority}</span>
            </td>
            <td>{formatDate(task.dueDate)}</td>
            <td>
              {task.isCompleted
                ? `Completed ${formatDate(task.completedOn!)}`
                : task.isOverdue
                  ? <strong>Overdue</strong>
                  : 'Pending'}
            </td>
            <td>
              {!task.isCompleted && (
                <button type="button" onClick={() => onComplete(task.id)} disabled={busyTaskId !== null}>
                  {busyTaskId === task.id ? 'Completing…' : 'Complete'}
                </button>
              )}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
