export interface BiometricReading {
  timestamp: number
  hr:        number | null  // bpm — live sensor
  resp:      number | null  // br/min
  skinTemp:  number | null  // °C
  sweatLoss: number | null  // ml (cumulative)
  stress:    number | null  // 0–100
  spo2:      number | null  // % blood oxygen
}

export type MetricKey = Exclude<keyof BiometricReading, 'timestamp'>

export interface MetricConfig {
  key:              MetricKey
  label:            string
  labelDe:          string
  unit:             string
  description:      string
  descriptionDe:    string
  subdescription:   string
  subdescriptionDe: string
  source:           'live' | 'derived'
  min:              number
  max:              number
  highThreshold:    number
  lowThreshold?:    number
  color:            string
}

export const METRICS: MetricConfig[] = [
  {
    key:              'hr',
    label:            'Heart Rate',
    labelDe:          'Herzfrequenz',
    unit:             'bpm',
    description:      'Beats per minute',
    descriptionDe:    'Schläge pro Minute',
    subdescription:   'Cardiovascular strain',
    subdescriptionDe: 'Kardiovaskuläre Belastung',
    source:           'live',
    min:              40,
    max:              180,
    highThreshold:    100,
    lowThreshold:     50,
    color:            '#ef4444',
  },
  {
    key:              'resp',
    label:            'Respiration',
    labelDe:          'Atmung',
    unit:             'br/min',
    description:      'Breaths per minute',
    descriptionDe:    'Atemzüge pro Minute',
    subdescription:   'Respiratory rate',
    subdescriptionDe: 'Atemfrequenz',
    source:           'derived',
    min:              8,
    max:              40,
    highThreshold:    20,
    lowThreshold:     10,
    color:            '#60a5fa',
  },
  {
    key:              'skinTemp',
    label:            'Skin Temp',
    labelDe:          'Hauttemperatur',
    unit:             '°C',
    description:      'Body surface temp in Celsius',
    descriptionDe:    'Körpertemperatur in Celsius',
    subdescription:   'Thermoregulation index',
    subdescriptionDe: 'Thermoregulationsindex',
    source:           'derived',
    min:              36.0,
    max:              37.5,
    highThreshold:    37.2,
    lowThreshold:     36.2,
    color:            '#fb923c',
  },
  {
    key:              'stress',
    label:            'Stress Level',
    labelDe:          'Stresslevel',
    unit:             '',
    description:      'Autonomic arousal score',
    descriptionDe:    'Autonomes Erregungsniveau',
    subdescription:   '0 – 100 scale',
    subdescriptionDe: 'Skala 0 – 100',
    source:           'derived',
    min:              0,
    max:              100,
    highThreshold:    50,
    color:            '#c084fc',
  },
  {
    key:              'sweatLoss',
    label:            'Sweat Loss',
    labelDe:          'Schweißverlust',
    unit:             'ml',
    description:      'Cumulative fluid loss',
    descriptionDe:    'Kumulativer Flüssigkeitsverlust',
    subdescription:   'Threshold: HR > 110 bpm',
    subdescriptionDe: 'Schwellenwert: HF > 110 bpm',
    source:           'derived',
    min:              0,
    max:              50,
    highThreshold:    20,
    color:            '#22d3ee',
  },
  {
    key:              'spo2',
    label:            'SpO₂',
    labelDe:          'Sauerstoffsättigung',
    unit:             '%',
    description:      'Blood oxygen saturation',
    descriptionDe:    'Blutsauerstoffsättigung',
    subdescription:   '97 – 100% normal range',
    subdescriptionDe: 'Normalbereich 97 – 100 %',
    source:           'derived',
    min:              94,
    max:              100,
    highThreshold:    100,
    lowThreshold:     95,
    color:            '#34d399',
  },
]
