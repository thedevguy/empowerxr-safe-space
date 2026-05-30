import { useEffect, useRef } from 'react'
import type { BiometricReading } from '../types/biometrics'

export type ConnectionStatus = 'connecting' | 'connected' | 'disconnected'

const WS_URL = (import.meta.env.VITE_WS_URL as string | undefined) ?? 'ws://localhost:8080'

export function useWebSocket(
  onReading: (r: BiometricReading) => void,
  onStatus:  (s: ConnectionStatus) => void,
) {
  const readingRef = useRef(onReading)
  const statusRef  = useRef(onStatus)
  useEffect(() => { readingRef.current = onReading })
  useEffect(() => { statusRef.current  = onStatus })

  useEffect(() => {
    let ws: WebSocket | null = null
    let retryTimer: ReturnType<typeof setTimeout> | null = null
    let cancelled = false

    function connect() {
      statusRef.current('connecting')
      ws = new WebSocket(WS_URL)

      ws.onopen = () => {
        if (!cancelled) statusRef.current('connected')
      }

      ws.onmessage = ({ data }) => {
        if (cancelled) return
        try {
          readingRef.current(JSON.parse(data as string) as BiometricReading)
        } catch {
          console.warn('[ws] unparseable message:', data)
        }
      }

      ws.onclose = () => {
        if (cancelled) return
        statusRef.current('disconnected')
        retryTimer = setTimeout(connect, 3_000)
      }

      ws.onerror = () => ws?.close()
    }

    connect()

    return () => {
      cancelled = true
      ws?.close()
      if (retryTimer) clearTimeout(retryTimer)
    }
  }, [])
}
