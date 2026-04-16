using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

[DefaultExecutionOrder(200)]
public class NetworkHandler : MonoBehaviour
{
    [Header("Network Data")]
    public string pc_ip = "192.168.0.100";
    public int dataPort = 5005;
    public int commandPort = 5006;

    [Header("Visual Hands (Arthritic)")]
    public OVRSkeleton visualLeft;
    public OVRSkeleton visualRight;

    [Header("Real Hands (Invisible/Tracked)")]
    public OVRSkeleton realLeft;
    public OVRSkeleton realRight;

    [Header("Arhtritis Visualizer Scripts for each hand")]
    public ArthritisHandVisualizer leftHand;
    public ArthritisHandVisualizer rightHand;

    [Header("Marbles")]
    public List<Transform> marbles = new List<Transform>();

    // data sender vars
    private UdpClient dataSender;
    private IPEndPoint targetEndPoint;
    private StringBuilder sb;

    // command receiver vars
    private UdpClient commandListener;
    private Thread commandThread;
    private bool running = false;
    private string latestMessage = "";
    private bool triggerReset = false; // used to trigger a marble reset

    void Start()
    {
        sb = new StringBuilder(2048);
        dataSender = new UdpClient();
        targetEndPoint = new IPEndPoint(IPAddress.Parse(pc_ip), dataPort);

        commandListener = new UdpClient(commandPort);
        running = true;
        commandThread = new Thread(new ThreadStart(ReceiveData));
        commandThread.IsBackground = true;
        commandThread.Start();
    }

    void Update()
    {
        if (!string.IsNullOrEmpty(latestMessage))
        {
            HandleMessage(latestMessage);
            latestMessage = "";
        }
    }

    void LateUpdate()
    {
        if (triggerReset)
        {
            ResetMarbles();
            triggerReset = false;
        }

        // Ensure skeletons are ready
        if (visualLeft is null || !visualLeft.IsInitialized ||
            visualRight is null || !visualRight.IsInitialized ||
            realLeft is null || !realLeft.IsInitialized ||
            realRight is null || !realRight.IsInitialized)
            return;

        // Build and send the packet
        sb.Clear();
        sb.Append(Time.time).Append("|");
        AppendSkeletonData(realLeft, "TL");
        AppendSkeletonData(realRight, "TR");
        AppendSkeletonData(visualLeft, "VL");
        AppendSkeletonData(visualRight, "VR");

        byte[] data = Encoding.UTF8.GetBytes(sb.ToString());
        dataSender.Send(data, data.Length, targetEndPoint);
    }

    public void ResetMarbles()
    {
        foreach (Transform marble in marbles)
        {
            marble.GetComponent<MarbleReset>().ResetPosition();
        }
    }

    void HandleMessage(string msg)
    {
        if (msg.StartsWith("SPEED:"))
        {
            string[] speeds = msg.Substring(6).Split("|");
            leftHand.fingerRotationSpeed = float.Parse(speeds[0]);
            rightHand.fingerRotationSpeed = float.Parse(speeds[1]);
        }
        else if (msg == "CMD:RESET")
        {
            triggerReset = true;
        }
    }

    void ReceiveData()
    {
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, commandPort);

        while (running)
        {
            try
            {
                byte[] data = commandListener.Receive(ref anyIP);
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

    private void AppendSkeletonData(OVRSkeleton skeleton, string handPrefix)
    {
        foreach (var bone in skeleton.Bones)
        {
            Transform t = bone.Transform;
            sb.Append(handPrefix).Append(",")
              .Append((int)bone.Id).Append(",")
              .Append(t.position.x).Append(",")
              .Append(t.position.y).Append(",")
              .Append(t.position.z).Append(",")
              .Append(t.rotation.x).Append(",")
              .Append(t.rotation.y).Append(",")
              .Append(t.rotation.z).Append(",")
              .Append(t.rotation.w).Append("|");
        }
    }

    void OnDestroy()
    {
        running = false;
        if (dataSender != null) dataSender.Close();
        if (commandListener != null) commandListener.Close();
        if (commandThread != null) commandThread.Abort();
    }
}
