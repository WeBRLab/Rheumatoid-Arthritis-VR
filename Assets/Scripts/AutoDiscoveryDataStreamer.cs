using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(200)]
public class AutoDiscoveryDataStreamer : MonoBehaviour
{
    [Header("Network Ports")]
    public int dataPort = 5005;
    public int beaconPort = 5006;
    private const string MAGIC_WORD = "VR_LOGGER_HERE";

    [Header("Hand References")]
    public OVRSkeleton leftHandSkeleton;
    public OVRSkeleton rightHandSkeleton;

    // Networking variables
    private UdpClient beaconListener;
    private UdpClient dataSender;
    private IPEndPoint targetEndPoint;
    private StringBuilder sb;

    // State management
    private bool isSearching = true;
    private bool isConnected = false;
    private string discoveredIP = "";

    void Start()
    {
        sb = new StringBuilder(2048);
        StartListeningForBeacon();
    }

    private void StartListeningForBeacon()
    {
        try
        {
            beaconListener = new UdpClient(beaconPort);
            Debug.Log($"[Auto-Discovery] Listening for beacon on port {beaconPort}...");
            
            // Start listening asynchronously (does not block the main Unity thread)
            beaconListener.BeginReceive(OnBeaconReceived, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Auto-Discovery] Failed to bind to port: {e.Message}");
        }
    }

    // This callback happens on a BACKGROUND thread when a packet arrives
    private void OnBeaconReceived(IAsyncResult res)
    {
        if (!isSearching) return;

        IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, beaconPort);
        byte[] receivedBytes = beaconListener.EndReceive(res, ref remoteEndpoint);
        string message = Encoding.UTF8.GetString(receivedBytes);

        if (message == MAGIC_WORD)
        {
            // We found the Python server!
            discoveredIP = remoteEndpoint.Address.ToString();
            Debug.Log($"[Auto-Discovery] Found Logger at {discoveredIP}");
            
            // Flag state changes so the main thread can take over
            isSearching = false;
            isConnected = true;
        }
        else
        {
            // False alarm, keep listening
            beaconListener.BeginReceive(OnBeaconReceived, null);
        }
    }

    void LateUpdate()
    {
        // Handle the transition from Searching -> Streaming on the main thread
        if (isConnected && dataSender == null)
        {
            // Initialize the sender now that we have the IP
            dataSender = new UdpClient();
            targetEndPoint = new IPEndPoint(IPAddress.Parse(discoveredIP), dataPort);
            
            // Close the listener so we free up the port
            if (beaconListener != null)
            {
                beaconListener.Close();
                beaconListener = null;
            }
        }

        // If we aren't fully connected and initialized yet, do nothing
        if (!isConnected || dataSender == null) return;

        // Ensure skeletons are ready
        if (leftHandSkeleton is not null || !leftHandSkeleton.IsInitialized ||
            rightHandSkeleton is not null || !rightHandSkeleton.IsInitialized) 
            return;

        // Build and send the packet
        sb.Clear();
        sb.Append(Time.time).Append("|");
        AppendSkeletonData(leftHandSkeleton, "L");
        AppendSkeletonData(rightHandSkeleton, "R");

        byte[] data = Encoding.UTF8.GetBytes(sb.ToString());
        dataSender.Send(data, data.Length, targetEndPoint);
    }

    private void AppendSkeletonData(OVRSkeleton skeleton, string handPrefix)
    {
        foreach (var bone in skeleton.Bones)
        {
            Transform t = bone.Transform;
            sb.Append(handPrefix).Append(",")
                .Append((int)bone.Id).Append(",")
                // Adding "F4" truncates the string to 4 decimal places (e.g., 0.1234)
                .Append(t.position.x.ToString("F4")).Append(",")
                .Append(t.position.y.ToString("F4")).Append(",")
                .Append(t.position.z.ToString("F4")).Append(",")
                .Append(t.rotation.x.ToString("F4")).Append(",")
                .Append(t.rotation.y.ToString("F4")).Append(",")
                .Append(t.rotation.z.ToString("F4")).Append(",")
                .Append(t.rotation.w.ToString("F4")).Append("|");
        }
    }

    void OnDestroy()
    {
        if (beaconListener != null) beaconListener.Close();
        if (dataSender != null) dataSender.Close();
    }
}