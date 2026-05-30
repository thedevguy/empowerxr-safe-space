import type { BiometricReading } from '../types/biometrics'

function clamp(val: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, val))
}

// Pass-through proxy: only fills fields that arrive as null.
// When the WearOS_Bridge sends pre-computed values, those are used as-is.
export class SmartProxy {
  // Stress state machine — initialises at 30 per project spec
  private stress   = 30
  // Skin temp random walk — initialises at 36.8°C per project spec
  private skinTemp = 36.8
  // Sweat loss accumulator — only increments above 110 bpm
  private sweat    = 0

  apply(reading: BiometricReading): BiometricReading {
    if (reading.hr == null) return reading

    const hr     = reading.hr
    const result = { ...reading }

    // ── Respiration: HR / 4, clamped 12–30 br/min ────────────────────────────
    if (result.resp == null) {
      result.resp = parseFloat(clamp(hr / 4, 12, 30).toFixed(1))
    }

    // ── Stress: incremental state machine ────────────────────────────────────
    // Climbs +2/tick when HR > 85; decays toward 30 when HR ≤ 85
    if (result.stress == null) {
      if (hr > 85) {
        this.stress = Math.min(100, this.stress + 2)
      } else {
        this.stress = Math.max(30, this.stress - 0.5)
      }
      result.stress = Math.round(this.stress)
    }

    // ── Skin temperature: constrained random walk ─────────────────────────────
    // ±0.1°C per tick, strictly clamped 36.1–37.4°C
    if (result.skinTemp == null) {
      this.skinTemp = clamp(
        this.skinTemp + (Math.random() - 0.5) * 0.2,
        36.1,
        37.4,
      )
      result.skinTemp = parseFloat(this.skinTemp.toFixed(1))
    }

    // ── Sweat loss: threshold accumulator ─────────────────────────────────────
    // Only accumulates (+0.5 ml/tick) when HR > 110; never decreases
    if (result.sweatLoss == null) {
      if (hr > 110) {
        this.sweat = Math.min(200, this.sweat + 0.5)
      }
      result.sweatLoss = parseFloat(this.sweat.toFixed(1))
    }

    // ── SpO₂: random fluctuation 97–99% ──────────────────────────────────────
    if (result.spo2 == null) {
      result.spo2 = 97 + Math.round(Math.random() * 2)
    }

    return result
  }
}
