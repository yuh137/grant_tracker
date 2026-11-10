import type { GrantSummary } from '../api.ts'

export type StatusFilter = 'Open' | 'Closed' | 'All'

interface Props {
  filter: StatusFilter
  grants: GrantSummary[]
  selectedId: string
  onFilterChange: (filter: StatusFilter) => void
  onSelect: (id: string) => void
}

export function GrantPicker({ filter, grants, selectedId, onFilterChange, onSelect }: Props) {
  return (
    <div className="picker">
      <label>
        Show
        <select value={filter} onChange={(e) => onFilterChange(e.target.value as StatusFilter)}>
          <option value="Open">Open grants</option>
          <option value="Closed">Closed grants</option>
          <option value="All">All grants</option>
        </select>
      </label>
      <label>
        Grant
        <select value={selectedId} onChange={(e) => onSelect(e.target.value)} disabled={grants.length === 0}>
          {grants.map((g) => (
            <option key={g.id} value={g.id}>
              {g.reference} · {g.organisationName} · {g.pendingTaskCount} pending
            </option>
          ))}
        </select>
      </label>
    </div>
  )
}
