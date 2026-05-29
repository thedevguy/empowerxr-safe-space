import dgram from 'dgram'
import fs    from 'fs'
import os    from 'os'
import path  from 'path'
import { fileURLToPath } from 'url'
import { WebSocketServer, WebSocket } from 'ws'

const __dirname = path.dirname(fileURLToPath(import.meta.url))

const UDP_PORT = Number(process.env.UDP_PORT) || 5000
const WS_PORT  = Number(process.env.WS_PORT)  || 8080

// ---------------------------------------------------------------------------
// Logger — writes to stdout and to logs/relay-<timestamp>.log
// ---------------------------------------------------------------------------
const logsDir = path.join(__dirname, 'logs')
if (!fs.existsSync(logsDir)) fs.mkdirSync(logsDir)

const logFileName = `relay-${new Date().toISOString().replace(/[:.]/g, '-')}.log`
const logStream   = fs.createWriteStream(path.join(logsDir, logFileName), { flags: 'a' })

function write(level, ...args) {
  const line = `${new Date().toISOString()} [${level}] ${args.join(' ')}`
  process.stdout.write(line + '\n')
  logStream.write(line + '\n')
}

const log = {
  info:  (...a) => write('INFO ', ...a),
  warn:  (...a) => write('WARN ', ...a),
  error: (...a) => write('ERROR', ...a),
}

log.info(`Log file: logs/${logFileName}`)

// ---------------------------------------------------------------------------
// Parsing
// Handles JSON first, falls back to comma-separated key=value pairs.
// ---------------------------------------------------------------------------
function parse(raw) {
  try {
    return JSON.parse(raw)
  } catch {
    const result = {}
    for (const pair of raw.split(',')) {
      const eq = pair.indexOf('=')
      if (eq === -1) continue
      const key = pair.slice(0, eq).trim()
      const val = pair.slice(eq + 1).trim()
      result[key] = Number.isFinite(Number(val)) ? Number(val) : val
    }
    return Object.keys(result).length ? result : null
  }
}

// ---------------------------------------------------------------------------
// Normalise field names
// ---------------------------------------------------------------------------
function normalise(raw) {
  return {
    hr:        raw.hr          ?? raw.heartRate    ?? null,
    resp:      raw.resp        ?? raw.respRate     ?? null,
    skinTemp:  raw.skinTemp    ?? raw.skin_temp    ?? raw.skin        ?? null,
    sweatLoss: raw.sweatLoss   ?? raw.sweat_loss   ?? raw.sweat       ?? null,
    stress:    raw.stress      ?? raw.stress_level ?? raw.stressLevel ?? null,
    spo2:      raw.spo2        ?? raw.spO2         ?? raw.SpO2        ?? null,
  }
}

// ---------------------------------------------------------------------------
// UDP receiver
// ---------------------------------------------------------------------------
const udp = dgram.createSocket({ type: 'udp4', reuseAddr: true })

udp.on('message', (msg, rinfo) => {
  const raw    = msg.toString().trim()
  log.info(`[udp] received from ${rinfo.address}:${rinfo.port} — ${raw}`)

  const parsed = parse(raw)
  if (!parsed) {
    log.warn(`[udp] unparseable message: ${raw}`)
    return
  }

  const packet = JSON.stringify({ timestamp: Date.now(), ...normalise(parsed) })

  let sent = 0
  wss.clients.forEach(client => {
    if (client.readyState === WebSocket.OPEN) {
      client.send(packet)
      sent++
    }
  })

  if (sent > 0) {
    log.info(`[udp→ws] forwarded to ${sent} client${sent > 1 ? 's' : ''}: ${packet}`)
  } else {
    log.info(`[udp] packet received but no ws clients connected`)
  }
})

udp.on('error', err => {
  log.error('[udp] socket error:', err.message)
  process.exit(1)
})

// Bind explicitly to 0.0.0.0 so subnet broadcasts on all interfaces are received
udp.bind({ port: UDP_PORT, address: '0.0.0.0', exclusive: false }, () => {
  udp.setBroadcast(true)
  log.info(`[udp] listening on 0.0.0.0:${UDP_PORT} — ready for subnet broadcasts`)

  const nets = os.networkInterfaces()
  for (const [iface, addrs] of Object.entries(nets)) {
    for (const addr of addrs) {
      if (addr.family === 'IPv4' && !addr.internal) {
        log.info(`[udp] interface ${iface} → ${addr.address}`)
      }
    }
  }

  log.info(`[udp] if no packets arrive, check that UDP ${UDP_PORT} is allowed inbound in your firewall`)
})

// ---------------------------------------------------------------------------
// WebSocket server
// ---------------------------------------------------------------------------
const wss = new WebSocketServer({ port: WS_PORT })

wss.on('listening', () => {
  log.info(`[ws]  server ready at ws://localhost:${WS_PORT}`)
})

wss.on('connection', (socket, req) => {
  const addr = req.socket.remoteAddress
  log.info(`[ws]  client connected: ${addr}  (total: ${wss.clients.size})`)

  socket.on('close', () => {
    log.info(`[ws]  client disconnected: ${addr}  (total: ${wss.clients.size})`)
  })

  socket.on('error', err => {
    log.warn(`[ws]  client error (${addr}): ${err.message}`)
  })
})

wss.on('error', err => {
  log.error('[ws]  server error:', err.message)
  process.exit(1)
})

// ---------------------------------------------------------------------------
// Graceful shutdown
// ---------------------------------------------------------------------------
function shutdown() {
  log.info('[relay] shutting down…')
  logStream.end()
  udp.close()
  wss.close(() => process.exit(0))
}

process.on('SIGINT',  shutdown)
process.on('SIGTERM', shutdown)
