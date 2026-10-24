import { Link } from '@tanstack/react-router'
import type { FileRouteTypes } from '@/routeTree.gen'
import { cn } from '@/lib/utils'

interface Segment {
  to: FileRouteTypes['to']
  label: string
}

/**
 * Segmented control whose segments are pages (.ia/design/components.md §SegmentedControl):
 * real links, so each mode has its own URL; the current one is marked with `aria-current`.
 */
export function SegmentedLinks({ segments, label }: { segments: Segment[]; label: string }) {
  return (
    <nav
      aria-label={label}
      className="grid auto-cols-fr grid-flow-col gap-1 rounded-lg bg-track p-1"
    >
      {segments.map((segment) => (
        <Link
          key={segment.to}
          to={segment.to}
          className={cn(
            'flex h-11 items-center justify-center rounded-[9px] text-sm font-medium text-ink no-underline',
            '[&.active]:bg-surface [&.active]:font-semibold',
          )}
          activeProps={{ 'aria-current': 'page' }}
        >
          {segment.label}
        </Link>
      ))}
    </nav>
  )
}
