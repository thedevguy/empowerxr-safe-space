package com.empowerxr.wearosbridge

import android.Manifest
import android.content.Context
import android.content.pm.PackageManager
import android.hardware.Sensor
import android.hardware.SensorEvent
import android.hardware.SensorEventListener
import android.hardware.SensorManager
import android.os.Bundle
import android.util.Log
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
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
import androidx.core.content.ContextCompat
import androidx.lifecycle.lifecycleScope
import androidx.wear.compose.material.MaterialTheme
import androidx.wear.compose.material.Text
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import org.json.JSONObject
import java.net.DatagramPacket
import java.net.DatagramSocket
import java.net.InetAddress
import java.util.Locale
import kotlin.math.round
import kotlin.math.roundToInt
import kotlin.random.Random

/** Rounds to one decimal place numerically, avoiding any locale-dependent string round-trip. */
private fun round1(value: Double): Double = round(value * 10.0) / 10.0

private const val TAG = "WearUdpBridge"
private const val BROADCAST_ADDRESS = "192.168.8.255"
private const val BROADCAST_PORT = 5000
private const val BROADCAST_INTERVAL_MS = 500L

data class Biometrics(
    val heartRate: Int,
    val respirationRate: Int,
    val skinTemp: Double,
    val sweatLoss: Double,
    val stressLevel: Int,
    val spo2: Int,
)

class MainActivity : ComponentActivity(), SensorEventListener {

    // Written on the sensor callback thread, read by the broadcast loop.
    @Volatile private var latestHeartRate: Int = 0

    private lateinit var sensorManager: SensorManager
    private var heartRateSensor: Sensor? = null
    private var isBroadcasting = false

    // Smart Proxy Engine state — all derived from live HR each tick.
    private var stressLevel: Int = 30
    private var sweatLoss: Double = 0.0
    private var skinTemp: Double = 36.8

    private val biometrics = MutableStateFlow(
        Biometrics(heartRate = 0, respirationRate = 0, skinTemp = 36.8,
            sweatLoss = 0.0, stressLevel = 30, spo2 = 98)
    )

    private val permissionLauncher =
        registerForActivityResult(ActivityResultContracts.RequestPermission()) { granted ->
            if (granted) registerHeartRateSensor()
        }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        sensorManager = getSystemService(Context.SENSOR_SERVICE) as SensorManager
        heartRateSensor = sensorManager.getDefaultSensor(Sensor.TYPE_HEART_RATE)
        ensurePermission()
        startBroadcasting()
        setContent {
            MaterialTheme {
                val sample by biometrics.collectAsState()
                BroadcasterScreen(sample)
            }
        }
    }

    override fun onResume() {
        super.onResume()
        if (hasPermission()) registerHeartRateSensor()
    }

    override fun onPause() {
        super.onPause()
        sensorManager.unregisterListener(this)
    }

    private fun hasPermission() = ContextCompat.checkSelfPermission(
        this, Manifest.permission.BODY_SENSORS
    ) == PackageManager.PERMISSION_GRANTED

    private fun ensurePermission() {
        if (hasPermission()) registerHeartRateSensor()
        else permissionLauncher.launch(Manifest.permission.BODY_SENSORS)
    }

    private fun registerHeartRateSensor() {
        heartRateSensor?.let {
            sensorManager.registerListener(this, it, SensorManager.SENSOR_DELAY_NORMAL)
        }
    }

    // SensorEventListener — fires on hardware HR updates.
    override fun onSensorChanged(event: SensorEvent) {
        if (event.sensor.type == Sensor.TYPE_HEART_RATE && event.values.isNotEmpty()) {
            latestHeartRate = event.values[0].roundToInt()
        }
    }

    override fun onAccuracyChanged(sensor: Sensor?, accuracy: Int) {}

    private fun startBroadcasting() {
        if (isBroadcasting) return
        isBroadcasting = true
        lifecycleScope.launch(Dispatchers.IO) {
            val broadcastAddress = InetAddress.getByName(BROADCAST_ADDRESS)
            DatagramSocket().use { socket ->
                socket.broadcast = true
                while (isActive) {
                    val hr = latestHeartRate

                    // Only run the proxy engine once we have a live HR reading.
                    if (hr > 0) {
                        // Respiration: HR / 4, clamped 12–30 br/min.
                        val resp = (hr / 4).coerceIn(12, 30)

                        // Stress (EDA proxy): state machine.
                        // HR > 85 → climb +2/tick toward 100.
                        // HR ≤ 85 → decay 5% of excess above 30 per tick back toward 30.
                        stressLevel = if (hr > 85) {
                            (stressLevel + 2).coerceAtMost(100)
                        } else {
                            val decay = ((stressLevel - 30) * 0.05).roundToInt().coerceAtLeast(1)
                            (stressLevel - decay).coerceAtLeast(30)
                        }

                        // Sweat loss: threshold accumulator. Only grows when HR > 110.
                        if (hr > 110) sweatLoss += 0.5

                        // Skin temperature: constrained random walk ±0.1 °C, clamped 36.1–37.4.
                        skinTemp = (skinTemp + Random.nextDouble(-0.1, 0.1))
                            .coerceIn(36.1, 37.4)

                        // SpO2: random fluctuation 97–99%.
                        val spo2 = Random.nextInt(97, 100)

                        val sample = Biometrics(
                            heartRate = hr,
                            respirationRate = resp,
                            skinTemp = skinTemp,
                            sweatLoss = sweatLoss,
                            stressLevel = stressLevel,
                            spo2 = spo2,
                        )
                        biometrics.value = sample

                        val payload = JSONObject().apply {
                            put("hr", hr)
                            put("resp", resp)
                            put("stress", stressLevel)
                            put("sweat_ml", round1(sweatLoss))
                            put("skin_temp", round1(skinTemp))
                            put("spo2", spo2)
                        }.toString()

                        try {
                            val bytes = payload.toByteArray(Charsets.UTF_8)
                            val packet = DatagramPacket(bytes, bytes.size, broadcastAddress, BROADCAST_PORT)
                            socket.send(packet)
                            Log.d(TAG, "Broadcast -> $payload")
                        } catch (e: Exception) {
                            Log.w(TAG, "Broadcast failed", e)
                        }
                    }

                    delay(BROADCAST_INTERVAL_MS)
                }
            }
        }
    }
}

@Composable
private fun BroadcasterScreen(sample: Biometrics) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(8.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center,
    ) {
        Text(
            text = if (sample.heartRate > 0) "HR: ${sample.heartRate} bpm" else "HR: waiting…",
            textAlign = TextAlign.Center,
            style = MaterialTheme.typography.title3,
        )
        Text(
            text = "Resp: ${sample.respirationRate} br/min",
            textAlign = TextAlign.Center,
            style = MaterialTheme.typography.body2,
        )
        Text(
            text = "Stress: ${sample.stressLevel}",
            textAlign = TextAlign.Center,
            style = MaterialTheme.typography.body2,
        )
        Text(
            text = "Skin: ${String.format(Locale.US, "%.1f", sample.skinTemp)} °C",
            textAlign = TextAlign.Center,
            style = MaterialTheme.typography.body2,
        )
        Text(
            text = "Sweat: ${String.format(Locale.US, "%.1f", sample.sweatLoss)} ml",
            textAlign = TextAlign.Center,
            style = MaterialTheme.typography.caption1,
        )
        Text(
            text = "SpO2: ${sample.spo2}%",
            textAlign = TextAlign.Center,
            style = MaterialTheme.typography.caption1,
        )
        Text(
            text = "Streaming…",
            textAlign = TextAlign.Center,
            style = MaterialTheme.typography.caption2,
        )
    }
}
