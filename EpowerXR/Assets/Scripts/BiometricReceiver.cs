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

    // All incoming biometric values
    public float heartRate = 0f;
    public float respiration = 0f;
    public float skinTemp = 0f;
    public float sweatLoss = 0f;
    public float stressLevel = 0f;
    public float spo2 = 0f;

    void Start()
    {
        udpClient = new UdpClient(port);
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log("UDP Listener started on port " + port);
    }

    private void ReceiveData()
    {
        IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
            byte[] bytes = udpClient.Receive(ref remoteEndpoint);
            string message = Encoding.UTF8.GetString(bytes);
            ParseMessage(message);
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
        receiveThread?.Abort();
        udpClient?.Close();
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