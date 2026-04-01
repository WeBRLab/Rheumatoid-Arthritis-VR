using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class FingerMoveSocket : MonoBehaviour
{
    [Header("Socket Data")]
    public string PC_IP = "192.168.0.2";
    public int Port = 5005;

    [Header("Assign the wrist bones of the hands")]
    public Transform left_wrist;
    public Transform right_wrist;
    public List<Transform> left_fingers;
    public List<Transform> right_fingers;

    private UdpClient udpc;
    private IPEndPoint ep;
    private float sendTimer;
    private Dictionary<Transform, Quaternion> previous_rot = new Dictionary<Transform, Quaternion>();

    void Start()
    {
        ep = new IPEndPoint(IPAddress.Parse(PC_IP), Port);
        udpc = new UdpClient();
        Debug.Log("UDP initialized");
    }

    void Update()
    {
        string moving = "";

        //Vector3 dist = finger.position - wrist.position;
        //moving += dist[1];

        foreach (Transform finger in left_fingers)
        {
            //Vector3 dist = finger.position - left_wrist.position;
            float x = Mathf.Pow(finger.position[0] - left_wrist.position[0], 2);
            float y = Mathf.Pow(finger.position[1] - left_wrist.position[1], 2);
            float z = Mathf.Pow(finger.position[2] - left_wrist.position[2], 2);
            float dist = Mathf.Sqrt(x + y + z);
            moving += finger.name + "=" + dist + ">";
        }

        foreach (Transform finger in right_fingers)
        {
            //Vector3 dist = finger.position - right_wrist.position;
            float x = Mathf.Pow(finger.position[0] - right_wrist.position[0], 2);
            float y = Mathf.Pow(finger.position[1] - right_wrist.position[1], 2);
            float z = Mathf.Pow(finger.position[2] - right_wrist.position[2], 2);
            float dist = Mathf.Sqrt(x + y + z);
            moving += finger.name + "=" + dist + ">";
        }

        sendTimer += Time.deltaTime;
        if (sendTimer > 0.02f)
        {
            sendTimer = 0f;
            if (moving != "")
            {
                Send(moving);
            }
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