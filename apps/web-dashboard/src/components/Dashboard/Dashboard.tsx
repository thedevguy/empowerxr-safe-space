import type { BiometricReading } from '../../types/biometrics'
import { METRICS } from '../../types/biometrics'
import WatchFace from '../WatchFace/WatchFace'
import MetricsGraph from '../MetricsGraph/MetricsGraph'
import SdkStateIndicator from '../SdkStateIndicator/SdkStateIndicator'
import MethodologyDrawer from '../MethodologyDrawer/MethodologyDrawer'
import './Dashboard.css'

interface Props {
  readings: BiometricReading[]
  latest:   BiometricReading | null
}

export default function Dashboard({ readings, latest }: Props) {
  return (
    <div className="dashboard">
      <header className="dashboard__header">
        <div>
          <h1 className="dashboard__title">
            <span className="dashboard__brand">EmpowerXR</span> SafeSpace Monitor
          </h1>
          <p className="dashboard__subtitle">Biometrisches Echtzeit-Überwachungssystem</p>
        </div>
      </header>

      <div className="dashboard__disclaimer">
        <p>
          <span className="disclaimer__badge disclaimer__badge--live">● LIVE</span>
          Heart Rate / Herzfrequenz — Samsung Galaxy Watch Ultra
        </p>
        <p>
          <span className="disclaimer__badge disclaimer__badge--proxy">● Smart Proxy</span>
          Respiration · Skin Temp · Stress · Sweat Loss · SpO₂ — real-time estimates derived from live HR
          <span className="disclaimer__de"> / Echtzeit-Schätzungen aus der Herzfrequenz abgeleitet</span>
        </p>
      </div>

      <SdkStateIndicator latest={latest} />

      <section className="dashboard__watches">
        {METRICS.map(config => (
          <WatchFace key={config.key} config={config} reading={latest} />
        ))}
      </section>

      <section className="dashboard__graph">
        <MetricsGraph readings={readings} />
      </section>

      <MethodologyDrawer />
    </div>
  )
}
