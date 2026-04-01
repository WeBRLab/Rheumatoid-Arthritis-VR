using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class SocketTest : MonoBehaviour
{
    [Header("Socket Data")]
    public string PC_IP = "192.168.0.67";
    public int Port = 5005;

    private UdpClient udpc;
    private IPEndPoint ep;
    private float sendTimer;
    // Start is called before the first frame update
    void Start()
    {
        ep = new IPEndPoint(IPAddress.Parse(PC_IP), Port);
        udpc = new UdpClient();
        Debug.Log("UDP initialized");
    }

    // Update is called once per frame
    void Update()
    {
        sendTimer += Time.deltaTime;
        if (sendTimer > 0.5f) // 10 Hz
        {
            sendTimer = 0f;
            Send("Hello World");
        }
    }

    void Send(string msg)
    {
        byte[] data = Encoding.UTF8.GetBytes(msg);
        udpc.Send(data, data.Length, ep);
    }

    void OnApplicationQuit()
    {
        udpc?.Close();
    }
}
