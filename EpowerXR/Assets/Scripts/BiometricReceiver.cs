using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class BiometricReceiver : MonoBehaviour
{
    private UdpClient udpClient;
    private Thread receiveThread;
    public int port = 5000;
    private bool isRunning = false;

    // All incoming biometric values
    public float heartRate = 0f;
    public float respiration = 0f;
    public float skinTemp = 0f;
    public float sweatLoss = 0f;
    public float stressLevel = 0f;
    public float spo2 = 0f;

    void Start()
    {
        try
        {
            udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            isRunning = true;
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            Debug.Log("UDP Listener started on port " + port);
        }
        catch (SocketException ex)
        {
            Debug.LogError("Failed to bind UDP socket on port " + port + ": " + ex.Message);
            isRunning = false;
        }
    }

    private void ReceiveData()
    {
        IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
        while (isRunning && udpClient != null)
        {
            try
            {
                byte[] bytes = udpClient.Receive(ref remoteEndpoint);
                string message = Encoding.UTF8.GetString(bytes);
                ParseMessage(message);
            }
            catch (SocketException ex) when (isRunning == false)
            {
                // Socket was closed intentionally, exit gracefully
                break;
            }
            catch (SocketException ex)
            {
                if (isRunning)
                    Debug.LogError("UDP Receive error: " + ex.Message);
                break;
            }
        }
    }

    private void ParseMessage(string message)
    {
        BiometricData data = JsonUtility.FromJson<BiometricData>(message);
        if (data != null)
        {
            heartRate = data.hr;
            respiration = data.resp;
            skinTemp = data.skin_temp;
            sweatLoss = data.sweat_loss;
            stressLevel = data.stress_level;
            spo2 = data.spo2;
        }
    }

    void OnDestroy()
    {
        isRunning = false;
        udpClient?.Close();
        udpClient?.Dispose();
    }
}

[System.Serializable]
public class BiometricData
{
    public float hr;
    public float resp;
    public float skin_temp;
    public float sweat_loss;
    public float stress_level;
    public float spo2;
}