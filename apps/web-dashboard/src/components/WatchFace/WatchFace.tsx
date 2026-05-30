import type { MetricConfig, BiometricReading } from '../../types/biometrics'
import './WatchFace.css'

interface Props {
  config:  MetricConfig
  reading: BiometricReading | null
}

type Status = 'Normal' | 'High' | 'Low' | 'Unknown'

const STATUS_DE: Record<Status, string> = {
  Normal:  'Normal',
  High:    'Hoch',
  Low:     'Niedrig',
  Unknown: 'Unbekannt',
}

function getStatus(value: number | null, config: MetricConfig): Status {
  if (value === null) return 'Unknown'
  if (value >= config.highThreshold) return 'High'
  if (config.lowThreshold !== undefined && value <= config.lowThreshold) return 'Low'
  return 'Normal'
}

function formatValue(value: number | null, key: MetricConfig['key']): string {
  if (value === null) return '—'
  if (key === 'hr' || key === 'stress') return Math.round(value).toString()
  return value.toFixed(1)
}

function bilingualStatus(status: Status): string {
  const de = STATUS_DE[status]
  return de !== status ? `${status} · ${de}` : status
}

export default function WatchFace({ config, reading }: Props) {
  const value  = reading?.[config.key] ?? null
  const status = getStatus(value, config)

  return (
    <div className="watch-face">
      <div className="watch-face__label-block">
        <span className="watch-face__label">{config.label}</span>
        <span className="watch-face__label-de">{config.labelDe}</span>
        <span className={`watch-face__source watch-face__source--${config.source}`}>
          {config.source === 'live' ? 'LIVE' : 'SMART PROXY'}
        </span>
      </div>

      <div className="watch-face__value-row">
        <span className="watch-face__value">{formatValue(value, config.key)}</span>
        {config.unit && (
          <span className="watch-face__unit">{config.unit}</span>
        )}
      </div>

      <div className="watch-face__desc-block">
        <span className="watch-face__description">{config.description}</span>
        <span className="watch-face__description-de">{config.descriptionDe}</span>
      </div>

      <div className="watch-face__desc-block">
        <span className="watch-face__subdescription">{config.subdescription}</span>
        <span className="watch-face__subdescription-de">{config.subdescriptionDe}</span>
      </div>

      <span className={`watch-face__status watch-face__status--${status.toLowerCase()}`}>
        {bilingualStatus(status)}
      </span>
    </div>
  )
}
