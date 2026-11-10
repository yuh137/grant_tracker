export type GrantStatus = 'Open' | 'Closed'
export type TaskPriority = 'Low' | 'Medium' | 'High'

export interface GrantSummary {
  id: string
  reference: string
  organisationName: string
  applicationDate: string
  status: GrantStatus
  pendingTaskCount: number
}

export interface TaskItem {
  id: string
  grantId: string
  title: string
  priority: TaskPriority
  dueDate: string
  createdOn: string
  completedOn: string | null
  isCompleted: boolean
  isOverdue: boolean
}

export interface GrantDetail {
  id: string
  reference: string
  organisationName: string
  applicationDate: string
  status: GrantStatus
  tasks: TaskItem[]
}

export interface NewTask {
  title: string
  priority: TaskPriority
  dueDate: string
}

export interface DailyTaskActivity {
  date: string
  created: number
  completed: number
}

export interface OverdueListSnapshot {
  totalTasks: number
  overdueTasks: number
  queryMilliseconds: number
}

export interface TaskActivityReport {
  from: string
  to: string
  days: DailyTaskActivity[]
  overdueList: OverdueListSnapshot
}

export interface Problem {
  status: number
  title: string
  detail?: string
  code?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  constructor(public readonly problem: Problem) {
    super(problem.detail ?? problem.title)
    this.name = 'ApiError'
  }
}

export class ConnectionError extends Error {
  constructor() {
    super('The server cannot be reached. Check your connection and try again.')
    this.name = 'ConnectionError'
  }
}

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(url, {
      ...init,
      headers: { accept: 'application/json', 'content-type': 'application/json', ...init?.headers },
    })
  } catch {
    throw new ConnectionError()
  }

  if (response.ok) {
    return (await response.json()) as T
  }

  if (response.status === 502 || response.status === 503 || response.status === 504) {
    throw new ConnectionError()
  }

  let problem: Problem
  try {
    problem = (await response.json()) as Problem
  } catch {
    if (response.status >= 500) {
      throw new ConnectionError()
    }
    problem = { status: response.status, title: response.statusText || `HTTP ${response.status}` }
  }
  throw new ApiError({ ...problem, status: problem.status ?? response.status })
}

export const api = {
  listGrants: (status?: GrantStatus) =>
    request<GrantSummary[]>(status ? `/grants?status=${status}` : '/grants'),
  getGrant: (id: string) => request<GrantDetail>(`/grants/${id}`),
  addTask: (grantId: string, task: NewTask) =>
    request<TaskItem>(`/grants/${grantId}/tasks`, { method: 'POST', body: JSON.stringify(task) }),
  completeTask: (taskId: string) => request<TaskItem>(`/tasks/${taskId}/complete`, { method: 'PUT' }),
  closeGrant: (grantId: string) => request<GrantSummary>(`/grants/${grantId}/close`, { method: 'PUT' }),
  taskActivity: (days: number) => request<TaskActivityReport>(`/reports/task-activity?days=${days}`),
}

export function todayIso(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

export function formatDate(iso: string): string {
  const [year, month, day] = iso.split('-')
  return `${day}/${month}/${year}`
}
