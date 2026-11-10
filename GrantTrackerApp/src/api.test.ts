import { afterEach, describe, expect, it, vi } from 'vitest'
import { api, ApiError, ConnectionError } from './api.ts'

afterEach(() => {
  vi.unstubAllGlobals()
})

function stubFetch(response: Response | Error) {
  vi.stubGlobal('fetch', vi.fn(() => (response instanceof Error ? Promise.reject(response) : Promise.resolve(response))))
}

describe('api error mapping', () => {
  it('turns a rejected fetch into a ConnectionError', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    await expect(api.listGrants()).rejects.toBeInstanceOf(ConnectionError)
  })

  it('treats a 5xx without a JSON body (a proxy in front of a dead API) as unreachable', async () => {
    stubFetch(new Response('Internal Server Error', { status: 500 }))

    await expect(api.completeTask('t1')).rejects.toBeInstanceOf(ConnectionError)
  })

  it('treats a bad gateway as unreachable', async () => {
    stubFetch(new Response('', { status: 502 }))

    await expect(api.listGrants()).rejects.toBeInstanceOf(ConnectionError)
  })

  it('keeps a ProblemDetails response as an ApiError with its code', async () => {
    stubFetch(
      new Response(JSON.stringify({ status: 422, title: 'Business rule violated', detail: 'Past', code: 'InvalidDueDate' }), {
        status: 422,
        headers: { 'content-type': 'application/problem+json' },
      }),
    )

    const error = await api.addTask('g1', { title: 'x', priority: 'Low', dueDate: '2000-01-01' }).catch((e) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).problem.code).toBe('InvalidDueDate')
  })
})
