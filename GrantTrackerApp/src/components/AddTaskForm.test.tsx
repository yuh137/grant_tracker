import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ApiError, todayIso } from '../api.ts'
import { AddTaskForm, validate } from './AddTaskForm.tsx'

function renderForm(onSubmit = vi.fn().mockResolvedValue(undefined)) {
  render(
    <AddTaskForm grantClosed={false} applicationDate="2020-01-01" onSubmit={onSubmit} onConnectionLost={vi.fn()} />,
  )
  return onSubmit
}

describe('validate', () => {
  it('flags a missing title, priority and due date', () => {
    const errors = validate('   ', '', '', '2020-01-01')
    expect(errors).toEqual({
      title: 'Title is required.',
      priority: 'Choose a priority.',
      dueDate: 'Due date is required.',
    })
  })

  it('accepts a 100 character title and rejects 101', () => {
    expect(validate('x'.repeat(100), 'Low', todayIso(), '2020-01-01')).toEqual({})
    expect(validate('x'.repeat(101), 'Low', todayIso(), '2020-01-01').title).toMatch(/100 characters/)
  })

  it('rejects a due date in the past and one before the application date', () => {
    expect(validate('Task', 'Low', '2000-01-01', '2020-01-01').dueDate).toBe('Due date cannot be in the past.')
    expect(validate('Task', 'Low', '2999-01-01', '3000-01-01').dueDate).toBe(
      'Due date cannot be before the application date.',
    )
  })
})

describe('AddTaskForm', () => {
  it('does not submit when the title is empty and shows the field error', async () => {
    const onSubmit = renderForm()
    await userEvent.selectOptions(screen.getByLabelText('Priority'), 'High')

    await userEvent.click(screen.getByRole('button', { name: 'Add task' }))

    expect(screen.getByText('Title is required.')).toBeInTheDocument()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('submits a trimmed task and clears the form', async () => {
    const onSubmit = renderForm()
    await userEvent.type(screen.getByLabelText('Title'), '  Verify bank details  ')
    await userEvent.selectOptions(screen.getByLabelText('Priority'), 'High')
    await userEvent.type(screen.getByLabelText('Due date'), todayIso())

    await userEvent.click(screen.getByRole('button', { name: 'Add task' }))

    expect(onSubmit).toHaveBeenCalledWith({ title: 'Verify bank details', priority: 'High', dueDate: todayIso() })
    expect(screen.getByLabelText('Title')).toHaveValue('')
  })

  it('shows the API business rule error with its code', async () => {
    const onSubmit = vi.fn().mockRejectedValue(
      new ApiError({
        status: 422,
        title: 'Business rule violated',
        detail: 'A task cannot be added to a closed grant.',
        code: 'GrantClosed',
      }),
    )
    renderForm(onSubmit)
    await userEvent.type(screen.getByLabelText('Title'), 'Late task')
    await userEvent.selectOptions(screen.getByLabelText('Priority'), 'Low')
    await userEvent.type(screen.getByLabelText('Due date'), todayIso())

    await userEvent.click(screen.getByRole('button', { name: 'Add task' }))

    expect(screen.getByRole('alert')).toHaveTextContent('A task cannot be added to a closed grant. (GrantClosed)')
    expect(screen.getByLabelText('Title')).toHaveValue('Late task')
  })
})
