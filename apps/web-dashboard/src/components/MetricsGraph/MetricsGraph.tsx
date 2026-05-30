import uPlot from 'uplot'
import 'uplot/dist/uPlot.min.css'
import { useRef, useEffect, useLayoutEffect, useMemo, useState } from 'react'
import type { BiometricReading, MetricConfig } from '../../types/biometrics'
import { METRICS } from '../../types/biometrics'
import './MetricsGraph.css'

interface Props {
  readings: BiometricReading[]
}

const WINDOW_SEC      = 5 * 60
const CHART_HEIGHT    = 160
const CURSOR_SYNC_KEY = 'safespace-metrics'

// ── Helpers ───────────────────────────────────────────────────────────────────

function formatRelTime(ts: number): string {
  const diffSec = Math.round(ts - Date.now() / 1000)
  if (Math.abs(diffSec) < 3) return 'now'
  const absSec = Math.abs(diffSec)
  const m = Math.floor(absSec / 60)
  const s = absSec % 60
  return `−${m}:${s.toString().padStart(2, '0')}`
}

function formatMetricValue(val: number | null, config: MetricConfig): string {
  if (val == null) return '—'
  const str = config.key === 'skinTemp' ? val.toFixed(1) : Math.round(val).toString()
  return config.unit ? `${str} ${config.unit}` : str
}

// ── Chart options ─────────────────────────────────────────────────────────────

function buildChartOpts(
  width:    number,
  metric:   MetricConfig,
  onCursor: (idx: number | null) => void,
): uPlot.Options {
  return {
    width,
    height:  CHART_HEIGHT,
    padding: [8, 12, 0, 0],
    scales: {
      x: {
        time: true,
        range: () => {
          const now = Date.now() / 1000
          return [now - WINDOW_SEC, now] as uPlot.Range.MinMax
        },
      },
      y: { range: () => [metric.min, metric.max] as uPlot.Range.MinMax },
    },
    axes: [
      {
        stroke: '#71717a',
        grid:   { stroke: '#1e1e1e', width: 1 },
        ticks:  { stroke: '#2a2a2a', width: 1 },
        font:   '10px ui-sans-serif, system-ui, sans-serif',
        splits: () => {
          const now = Date.now() / 1000
          return [-5, -4, -3, -2, -1, 0].map(m => now + m * 60)
        },
        values: (_u, splits) =>
          splits.map(v => {
            if (v == null) return ''
            const d = v - Date.now() / 1000
            if (Math.abs(d) < 30) return 'now'
            return `${Math.round(d / 60)} min`
          }),
      },
      {
        stroke: '#71717a',
        grid:   { stroke: '#1e1e1e', width: 1 },
        ticks:  { stroke: '#2a2a2a', width: 1 },
        font:   '10px ui-sans-serif, system-ui, sans-serif',
        size:   44,
        values: (_u, splits) =>
          splits.map(v => {
            if (v == null) return ''
            return metric.key === 'skinTemp' ? v.toFixed(1) : Math.round(v).toString()
          }),
      },
    ],
    series: [
      {},
      {
        label:    metric.label,
        stroke:   metric.color,
        width:    1.5,
        spanGaps: false,
        points:   { show: true, size: 3, fill: metric.color, stroke: metric.color },
      },
    ],
    legend: { show: false },
    cursor: {
      x: true,
      y: false,                  // no horizontal crosshair
      points: { size: 9 },       // large highlight dot at cursor position
      sync:   { key: CURSOR_SYNC_KEY },
      drag:   { x: false, y: false, setScale: false },
    },
    hooks: {
      setCursor: [(u: uPlot) => onCursor(u.cursor.idx ?? null)],
    },
  }
}

// ── Individual metric chart ───────────────────────────────────────────────────

interface MetricChartProps {
  config:   MetricConfig
  readings: BiometricReading[]
}

function MetricChart({ config, readings }: MetricChartProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const uplotRef     = useRef<uPlot | null>(null)
  const [cursorIdx, setCursorIdx] = useState<number | null>(null)

  const chartData = useMemo(() => {
    if (readings.length === 0) return [[], []] as uPlot.AlignedData
    return [
      readings.map(r => r.timestamp / 1000),
      readings.map(r => r[config.key]),
    ] as uPlot.AlignedData
  }, [readings, config.key])

  // Derive what to display — cursor position while hovering, latest otherwise
  const { displayTime, displayValue } = useMemo(() => {
    const isHovering = cursorIdx !== null
    const data0 = chartData[0] as number[]
    const data1 = chartData[1] as (number | null)[]

    let ts:  number | null = null
    let val: number | null = null

    if (isHovering && cursorIdx < data0.length) {
      ts  = data0[cursorIdx]
      val = data1[cursorIdx]
    } else if (data0.length > 0) {
      ts  = data0[data0.length - 1]
      val = data1[data1.length - 1]
    }

    return {
      displayTime:  isHovering && ts != null ? formatRelTime(ts) : null,
      displayValue: formatMetricValue(val, config),
    }
  }, [cursorIdx, chartData, config])

  useLayoutEffect(() => {
    if (!containerRef.current) return

    const opts = buildChartOpts(
      containerRef.current.clientWidth,
      config,
      idx => setCursorIdx(idx),
    )
    uplotRef.current = new uPlot(opts, chartData, containerRef.current)

    const ro = new ResizeObserver(entries => {
      const w = entries[0].contentRect.width
      if (w > 0) uplotRef.current?.setSize({ width: w, height: CHART_HEIGHT })
    })
    ro.observe(containerRef.current)

    return () => {
      ro.disconnect()
      uplotRef.current?.destroy()
      uplotRef.current = null
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    uplotRef.current?.setData(chartData)
  }, [chartData])

  return (
    <div className="metric-chart">
      <div className="metric-chart__header">
        <div className="metric-chart__label-block" style={{ color: config.color }}>
          <span className="metric-chart__label">{config.label}</span>
          <span className="metric-chart__label-de">{config.labelDe}</span>
        </div>
        {config.unit && (
          <span className="metric-chart__unit">{config.unit}</span>
        )}
        <span className={`metric-chart__source metric-chart__source--${config.source}`}>
          {config.source === 'live' ? 'LIVE' : 'SMART PROXY'}
        </span>
      </div>

      <div ref={containerRef} />

      <div className="metric-chart__info">
        {/* Reserve space for time so the value line never jumps */}
        <span className="metric-chart__time">
          {displayTime ?? ' '}
        </span>
        <span className="metric-chart__reading" style={{ color: config.color }}>
          {displayValue}
        </span>
      </div>
    </div>
  )
}

// ── Grid container ────────────────────────────────────────────────────────────

export default function MetricsGraph({ readings }: Props) {
  return (
    <div className="metrics-graph">
      {METRICS.map(config => (
        <MetricChart key={config.key} config={config} readings={readings} />
      ))}
    </div>
  )
}
