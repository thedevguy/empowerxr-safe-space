import './MethodologyDrawer.css'

interface Entry {
  color:     string
  label:     string
  labelDe:   string
  en:        string
  de:        string
  formula:   string
}

const ENTRIES: Entry[] = [
  {
    color:   '#60a5fa',
    label:   'Respiration Rate',
    labelDe: 'Atemfrequenz',
    en: `Derived by direct division of heart rate, reflecting the well-documented physiological
coupling between cardiac and respiratory cycles. The result is clamped to the clinically
normal resting range of 12–30 breaths per minute.`,
    de: `Abgeleitet von der direkten Division der Herzfrequenz, basierend auf der physiologischen
Kopplung zwischen Herz- und Atemrhythmus. Das Ergebnis wird auf den klinisch normalen
Ruhewertbereich von 12–30 Atemzügen pro Minute begrenzt.`,
    formula: 'resp = clamp(HR / 4,  12,  30)',
  },
  {
    color:   '#fb923c',
    label:   'Skin Temperature',
    labelDe: 'Hauttemperatur',
    en: `Modelled as a constrained random walk to reflect the micro-fluctuations characteristic
of skin surface thermoregulation at rest. Each 500 ms tick applies a random delta of
±0.1°C, with hard clamps at 36.1°C and 37.4°C to keep values within the clinically
normal surface-temperature band.`,
    de: `Modelliert als begrenzter Zufallspfad zur Darstellung der für die Thermoregulation der
Hautoberfläche in Ruhe charakteristischen Mikroschwankungen. Jeder 500-ms-Tick erzeugt
ein zufälliges Delta von ±0,1°C, mit harten Grenzen bei 36,1°C und 37,4°C, um die Werte
im klinisch normalen Oberflächentemperaturbereich zu halten.`,
    formula: 'skinTemp = clamp(prev ± 0.1,  36.1,  37.4)   |   init: 36.8°C',
  },
  {
    color:   '#c084fc',
    label:   'Stress Level',
    labelDe: 'Stresslevel',
    en: `An incremental state machine that initialises at 30 — a resting autonomic baseline.
When HR crosses 85 bpm (the threshold for meaningful sympathetic activation), the score
climbs by 2 units per 500 ms tick toward a ceiling of 100. Below threshold, it decays by
0.5 units per tick back toward the resting baseline of 30. This directional lag mimics
autonomic nervous system build-up and recovery dynamics.`,
    de: `Eine inkrementelle Zustandsmaschine, die bei 30 initialisiert — einem autonomen Ruhewert.
Wenn die HF 85 bpm überschreitet (Schwelle für bedeutsame sympathische Aktivierung),
steigt der Wert um 2 Einheiten pro 500-ms-Tick bis zu einem Maximum von 100. Unterhalb
der Schwelle fällt er um 0,5 Einheiten pro Tick zurück auf den Ruhewert 30. Diese
Richtungsverzögerung simuliert die Aufbau- und Erholungsdynamik des autonomen Nervensystems.`,
    formula: 'if HR > 85: stress = min(100, stress + 2)   |   else: stress = max(30, stress − 0.5)   |   init: 30',
  },
  {
    color:   '#22d3ee',
    label:   'Sweat Loss',
    labelDe: 'Schweißverlust',
    en: `A threshold accumulator that reflects the clinical reality that meaningful perspiration
onset requires sustained exertion. The counter initialises at 0 ml and only increments
by 0.5 ml per 500 ms tick when HR exceeds 110 bpm — a moderate-to-high exertion threshold.
It never decreases, modelling the cumulative nature of fluid loss during physical activity.`,
    de: `Ein Schwellenakkumulator, der die klinische Realität widerspiegelt, dass relevanter
Schweißausbruch anhaltende Anstrengung erfordert. Der Zähler beginnt bei 0 ml und
erhöht sich nur um 0,5 ml pro 500-ms-Tick, wenn die HF 110 bpm überschreitet.
Er nimmt nie ab und modelliert den kumulativen Charakter des Flüssigkeitsverlusts.`,
    formula: 'if HR > 110: sweat += 0.5 ml   |   never decreases   |   init: 0 ml',
  },
  {
    color:   '#34d399',
    label:   'SpO₂ (Blood Oxygen)',
    labelDe: 'Sauerstoffsättigung',
    en: `Modelled as a random fluctuation within the clinically normal blood oxygen saturation
range for a healthy adult at rest. Each tick samples uniformly from 97–99%, consistent
with the normal SpO₂ range reported by Samsung's BioActive sensor on the Galaxy Watch.`,
    de: `Modelliert als zufällige Schwankung im klinisch normalen Blutsauerstoffsättigungsbereich
eines gesunden Erwachsenen im Ruhezustand. Jeder Tick sampelt gleichmäßig zwischen 97–99%,
konsistent mit dem normalen SpO₂-Bereich des Samsung BioActive Sensors.`,
    formula: 'spo2 = 97 + round(random() × 2)   |   range: 97–99%',
  },
]

const SHARED = {
  en: 'Fields pre-computed by the WearOS_Bridge are used as-is. The Smart Proxy only fills null fields.',
  de: 'Vom WearOS_Bridge vorberechnete Felder werden direkt verwendet. Der Smart Proxy füllt nur null-Felder auf.',
}

export default function MethodologyDrawer() {
  return (
    <details className="methodology">
      <summary className="methodology__toggle">
        <span className="methodology__toggle-label">
          Smart Proxy Methodology
          <span className="methodology__toggle-label-de"> / Smart-Proxy-Methodik</span>
        </span>
        <span className="methodology__chevron" aria-hidden>▸</span>
      </summary>

      <div className="methodology__body">
        <div className="methodology__entries">
          {ENTRIES.map(entry => (
            <div key={entry.label} className="methodology__entry">
              <div className="methodology__entry-title">
                <span className="methodology__dot" style={{ background: entry.color }} />
                <span className="methodology__entry-label">{entry.label}</span>
                <span className="methodology__entry-label-de"> / {entry.labelDe}</span>
              </div>
              <p className="methodology__text">{entry.en}</p>
              <p className="methodology__text methodology__text--de">{entry.de}</p>
              <code className="methodology__formula">{entry.formula}</code>
            </div>
          ))}
        </div>

        <div className="methodology__shared">
          <code className="methodology__formula">{SHARED.en}</code>
          <code className="methodology__formula methodology__formula--de">{SHARED.de}</code>
        </div>
      </div>
    </details>
  )
}
