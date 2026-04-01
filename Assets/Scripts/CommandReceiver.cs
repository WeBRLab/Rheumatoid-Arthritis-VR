using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class CommandReceiver : MonoBehaviour
{
    public int port = 5006;

    public ArthritisHandVisualizer leftVisual;
    public ArthritisHandVisualizer rightVisual;

    private UdpClient commandReceiver;
    private Thread receiveThread;
    private bool running = false;

    private string latestMessage = "";

    void Start()
    {
        commandReceiver = new UdpClient(port);
        running = true;

        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void ReceiveData()
    {
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, port);

        while (running)
        {
            try
            {
                byte[] data = commandReceiver.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);

                latestMessage = text;
                Debug.Log("Received: " + text);
            }
            catch (Exception err)
            {
                Debug.LogError(err.ToString());
            }
        }
    }

    void Update()
    {
        if (!string.IsNullOrEmpty(latestMessage))
        {
            HandleMessage(latestMessage);
            latestMessage = "";
        }
    }

    void HandleMessage(string msg)
    {
        string[] speeds = msg.Split("|");
        leftVisual.fingerRotationSpeed = int.Parse(speeds[0]);
        rightVisual.fingerRotationSpeed = int.Parse(speeds[1]);
    }

    void OnApplicationQuit()
    {
        running = false;

        if (receiveThread != null)
            receiveThread.Abort();

        if (commandReceiver != null)
            commandReceiver.Close();
    }
}
