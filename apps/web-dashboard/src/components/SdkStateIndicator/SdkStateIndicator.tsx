import { useMemo } from 'react'
import type { BiometricReading } from '../../types/biometrics'
import './SdkStateIndicator.css'

interface Props {
  latest: BiometricReading | null
}

// ── State model ───────────────────────────────────────────────────────────────

type SdkState = 'baseline' | 'elevated' | 'alert' | 'intervention'

interface StateConfig {
  label:         string
  labelDe:       string
  description:   string
  descriptionDe: string
  action:        string | null
  actionDe:      string | null
}

const STATE: Record<SdkState, StateConfig> = {
  baseline: {
    label:         'BASELINE',
    labelDe:       'NORMALZUSTAND',
    description:   'All biometrics within normal range. SDK monitoring passively.',
    descriptionDe: 'Alle Biometrie-Werte im Normalbereich. SDK überwacht passiv.',
    action:        null,
    actionDe:      null,
  },
  elevated: {
    label:         'ELEVATED',
    labelDe:       'ERHÖHT',
    description:   'Heart rate above resting threshold. Autonomic stress response beginning.',
    descriptionDe: 'Herzfrequenz über dem Ruheschwellenwert. Autonome Stressreaktion beginnt.',
    action:        'Passive visual softening active',
    actionDe:      'Passive visuelle Abschwächung aktiv',
  },
  alert: {
    label:         'ALERT',
    labelDe:       'WARNUNG',
    description:   'Elevated HR with confirmed autonomic arousal. Intervention threshold approaching.',
    descriptionDe: 'Erhöhte HF mit bestätigter autonomer Erregung. Eingreifschwelle nähert sich.',
    action:        'Colour lens + peripheral vignette engaged',
    actionDe:      'Farblinse + periphere Vignette aktiv',
  },
  intervention: {
    label:         'INTERVENTION',
    labelDe:       'EINGRIFF',
    description:   'Overwhelm threshold breached. SDK override active.',
    descriptionDe: 'Überwältigungsschwelle überschritten. SDK-Übernahme aktiv.',
    action:        'Passthrough mode forced · Grounding anchor deployed',
    actionDe:      'Passthrough-Modus erzwungen · Beruhigungsanker eingesetzt',
  },
}

// ── Trigger definitions ───────────────────────────────────────────────────────
// Each trigger maps to a documented threshold from the project spec.
// Active triggers are highlighted; inactive ones remain visible so observers
// can see what the SDK is waiting for before escalating.

interface Trigger {
  key:    string
  en:     string
  de:     string
  active: boolean
}

function buildTriggers(r: BiometricReading): Trigger[] {
  return [
    {
      key:    'hr-85',
      en:     'HR ≥ 85 bpm',
      de:     'HF ≥ 85 bpm',
      active: (r.hr ?? 0) >= 85,
    },
    {
      key:    'hr-110',
      en:     'HR ≥ 110 bpm',
      de:     'HF ≥ 110 bpm',
      active: (r.hr ?? 0) >= 110,
    },
    {
      key:    'stress-50',
      en:     'Stress ≥ 50',
      de:     'Stress ≥ 50',
      active: (r.stress ?? 0) >= 50,
    },
    {
      key:    'stress-65',
      en:     'Stress ≥ 65',
      de:     'Stress ≥ 65',
      active: (r.stress ?? 0) >= 65,
    },
    {
      key:    'resp-20',
      en:     'Resp ≥ 20 br/min',
      de:     'Atmung ≥ 20/min',
      active: (r.resp ?? 0) >= 20,
    },
    {
      key:    'sweat',
      en:     'Sweat accumulating',
      de:     'Schweißverlust akkumuliert',
      active: (r.sweatLoss ?? 0) > 0,
    },
  ]
}

// ── State computation (mirrors Unity SDK logic) ───────────────────────────────

function computeState(r: BiometricReading): SdkState {
  const hr    = r.hr        ?? 0
  const stress = r.stress   ?? 0
  const sweat  = r.sweatLoss ?? 0

  // Intervention: sustained high HR (sweat confirming duration) + critical stress
  if (hr >= 110 && sweat > 0 && stress >= 65) return 'intervention'

  // Alert: elevated HR with confirmed autonomic arousal
  if (hr >= 85 && stress >= 50) return 'alert'

  // Elevated: HR crossed the stress-machine threshold
  if (hr >= 85) return 'elevated'

  return 'baseline'
}

// ── Component ─────────────────────────────────────────────────────────────────

export default function SdkStateIndicator({ latest }: Props) {
  const state    = useMemo(() => latest ? computeState(latest)    : 'baseline', [latest])
  const triggers = useMemo(() => latest ? buildTriggers(latest)   : [],         [latest])
  const config   = STATE[state]

  return (
    <div className={`sdk-state sdk-state--${state}`}>

      <div className="sdk-state__main">
        <div className="sdk-state__identity">
          <div className="sdk-state__label">
            <span className="sdk-state__dot" />
            {config.label}
            <span className="sdk-state__label-de"> · {config.labelDe}</span>
          </div>
          <p className="sdk-state__description">
            {config.description}
            <span className="sdk-state__desc-de"> / {config.descriptionDe}</span>
          </p>
        </div>

        {config.action && (
          <div className="sdk-state__response">
            <span className="sdk-state__response-heading">
              SDK Response / Reaktion
            </span>
            <span className="sdk-state__response-value">
              {config.action}
              <span className="sdk-state__response-de"> / {config.actionDe}</span>
            </span>
          </div>
        )}
      </div>

      <div className="sdk-state__triggers">
        <span className="sdk-state__triggers-label">Active triggers / Aktive Auslöser</span>
        <div className="sdk-state__trigger-list">
          {triggers.map(t => (
            <span
              key={t.key}
              className={`sdk-state__trigger ${t.active ? 'sdk-state__trigger--on' : ''}`}
            >
              {t.en} / {t.de}
            </span>
          ))}
        </div>
      </div>

    </div>
  )
}
