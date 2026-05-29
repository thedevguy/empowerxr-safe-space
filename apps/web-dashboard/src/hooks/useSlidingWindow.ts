import { useState, useCallback } from 'react'
import type { BiometricReading } from '../types/biometrics'

const WINDOW_MS = 10 * 60 * 1_000

export function useSlidingWindow() {
  const [readings, setReadings] = useState<BiometricReading[]>([])

  const push = useCallback((reading: BiometricReading) => {
    setReadings(prev => {
      const cutoff = reading.timestamp - WINDOW_MS
      const base   = prev.length > 0 && prev[0].timestamp < cutoff
        ? prev.filter(r => r.timestamp >= cutoff)
        : prev
      return [...base, reading]
    })
  }, [])

  return { readings, push }
}
