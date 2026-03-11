using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using TMPro; // This is required to access TextMeshPro elements

[Serializable]
public class AppData 
{
    public float numValue;
    public string strValue;
}

public class UDPReceiver : MonoBehaviour
{
    [Header("Network Settings")]
    public int port = 5005;

    [Header("TextMeshPro Displays")]
    [Tooltip("Drag the TMP object that will display the number here")]
    public TMP_Text numberDisplayText; 
    
    [Tooltip("Drag the TMP object that will display the string here")]
    public TMP_Text stringDisplayText;

    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = true;

    // Thread-safe variables for passing data to the main thread
    private AppData latestData = new AppData();
    private bool dataUpdated = false;
    private readonly object dataLock = new object();

    void Start()
    {
        // Start the background thread for listening
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log($"UDP Listener started on port {port}");
    }

    private void ReceiveData()
    {
        udpClient = new UdpClient(port);
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, port);

        while (isRunning)
        {
            try
            {
                // This will block until a packet arrives
                byte[] data = udpClient.Receive(ref anyIP);
                string jsonString = Encoding.UTF8.GetString(data);
                
                // Parse the JSON into our C# class
                AppData parsedData = JsonUtility.FromJson<AppData>(jsonString);

                // Lock the data so the main thread doesn't read it while it's being written
                lock (dataLock)
                {
                    latestData = parsedData;
                    dataUpdated = true;
                }
            }
            catch (SocketException)
            {
                if (isRunning) Debug.LogWarning("UDP Socket Exception occurred.");
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }
    }

    void Update()
    {
        // Safely check if new data arrived from the background thread
        lock (dataLock)
        {
            if (dataUpdated)
            {
                // Map the data directly to the TMP elements if they are assigned
                if (numberDisplayText != null)
                {
                    // F2 formats the float to 2 decimal places. Remove it if you want the raw float.
                    numberDisplayText.text = $"Number: {latestData.numValue:F2}"; 
                }

                if (stringDisplayText != null)
                {
                    stringDisplayText.text = $"String: {latestData.strValue}";
                }

                dataUpdated = false;
            }
        }
    }

    void OnDestroy()
    {
        // Clean up the thread and socket gracefully when the app closes
        isRunning = false;
        if (udpClient != null)
        {
            udpClient.Close();
        }
    }
}