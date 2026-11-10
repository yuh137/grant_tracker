interface Props {
  onRetry: () => void
}

export function ConnectionBanner({ onRetry }: Props) {
  return (
    <div className="banner" role="alert">
      <span>The server cannot be reached. Your last action may not have been saved.</span>
      <button type="button" onClick={onRetry}>
        Retry
      </button>
    </div>
  )
}
