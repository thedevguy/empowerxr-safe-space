import { useState, useCallback, useRef } from 'react'
import { useSlidingWindow } from './hooks/useSlidingWindow'
import { useWebSocket, type ConnectionStatus } from './hooks/useWebSocket'
import { SmartProxy } from './proxy/smartProxy'
import type { BiometricReading } from './types/biometrics'
import Dashboard from './components/Dashboard/Dashboard'

export default function App() {
  const { readings, push } = useSlidingWindow()
  const [_status, setStatus] = useState<ConnectionStatus>('disconnected')
  const proxy = useRef(new SmartProxy())

  const handleStatus   = useCallback((s: ConnectionStatus) => setStatus(s), [])
  const handleReading  = useCallback(
    (raw: BiometricReading) => push(proxy.current.apply(raw)),
    [push],
  )

  useWebSocket(handleReading, handleStatus)

  return (
    <Dashboard
      readings={readings}
      latest={readings[readings.length - 1] ?? null}
    />
  )
}
