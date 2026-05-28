package com.empowerxr.wearosbridge

import android.os.Bundle
import android.util.Log
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.lifecycleScope
import androidx.wear.compose.material.MaterialTheme
import androidx.wear.compose.material.Text
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import java.net.DatagramPacket
import java.net.DatagramSocket
import java.net.InetAddress
import kotlin.math.roundToInt
import kotlin.math.sin

private const val TAG = "WearUdpBridge"
private const val BROADCAST_ADDRESS = "255.255.255.255"
private const val BROADCAST_PORT = 5000
private const val BROADCAST_INTERVAL_MS = 500L

/** A single simulated biometric reading. */
data class Biometrics(val heartRate: Int, val respirationRate: Int)

class MainActivity : ComponentActivity() {

    // Latest simulated sample, observed by the Compose UI.
    private val biometrics = MutableStateFlow(Biometrics(heartRate = 70, respirationRate = 15))

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        startBroadcasting()

        setContent {
            MaterialTheme {
                val sample by biometrics.collectAsState()
                BroadcasterScreen(sample)
            }
        }
    }

    /**
     * Launches a background coroutine on the IO dispatcher that, every
     * [BROADCAST_INTERVAL_MS], simulates a heart rate (70–120 bpm) and
     * breathing rate (15–30 br/min), packages them into a JSON string, and
     * sends them as a UDP datagram to the subnet broadcast address.
     *
     * The coroutine is tied to [lifecycleScope], so it is automatically
     * cancelled (and the socket closed) when the activity is destroyed.
     */
    private fun startBroadcasting() {
        lifecycleScope.launch(Dispatchers.IO) {
            val broadcastAddress = InetAddress.getByName(BROADCAST_ADDRESS)
            DatagramSocket().use { socket ->
                socket.broadcast = true
                var tick = 0
                while (isActive) {
                    // Two out-of-phase sine waves keep the values gently oscillating.
                    val hr = oscillate(min = 70, max = 120, periodTicks = 40, tick = tick)
                    val resp = oscillate(min = 15, max = 30, periodTicks = 24, tick = tick)

                    biometrics.value = Biometrics(heartRate = hr, respirationRate = resp)

                    val payload = """{"hr": $hr, "resp": $resp}"""
                    try {
                        val bytes = payload.toByteArray(Charsets.UTF_8)
                        val packet = DatagramPacket(bytes, bytes.size, broadcastAddress, BROADCAST_PORT)
                        socket.send(packet)
                        Log.d(TAG, "Broadcast -> $payload")
                    } catch (e: Exception) {
                        // Networking can fail transiently (no route, etc.); keep simulating.
                        Log.w(TAG, "Failed to broadcast packet: $payload", e)
                    }

                    tick++
                    delay(BROADCAST_INTERVAL_MS)
                }
            }
        }
    }
}

/**
 * Maps [tick] onto a smooth sine oscillation bounded by [min]..[max], completing
 * one full cycle every [periodTicks] ticks.
 */
private fun oscillate(min: Int, max: Int, periodTicks: Int, tick: Int): Int {
    val mid = (min + max) / 2.0
    val amplitude = (max - min) / 2.0
    val phase = 2.0 * Math.PI * (tick % periodTicks) / periodTicks
    return (mid + amplitude * sin(phase)).roundToInt()
}

@Composable
private fun BroadcasterScreen(sample: Biometrics) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(8.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Text(
            text = "HR: ${sample.heartRate} bpm",
            textAlign = TextAlign.Center,
            style = MaterialTheme.typography.title2
        )
        Text(
            text = "Resp: ${sample.respirationRate} br/min",
            textAlign = TextAlign.Center,
            style = MaterialTheme.typography.body2
        )
        Text(
            text = "Broadcasting…",
            textAlign = TextAlign.Center,
            style = MaterialTheme.typography.caption1
        )
    }
}
