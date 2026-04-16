using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(200)]
public class AutoDiscoveryDataStreamer : MonoBehaviour
{
    [Header("Network Ports")]
    public string pc_ip = "192.168.0.100";
    public int dataPort = 5005;
    public int beaconPort = 5006;
    private const string MAGIC_WORD = "VR_LOGGER_HERE";

<<<<<<< Updated upstream
    [Header("Hand References")]
    public OVRSkeleton leftHandSkeleton;
    public OVRSkeleton rightHandSkeleton;

    // Networking variables
=======
    [Header("Simulation State")]
    [Tooltip("Read this value from your Arthritis script to apply the remote speed")]
    public float currentFingerSpeedLeft = 8.0f; // Python will remotely change this!
    public float currentFingerSpeedRight = 8.0f;
    //public float currentFingerSpeed = 8.0f;

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
    
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
        StartListeningForBeacon();
=======
        //StartListeningForBeacon();
        dataSender = new UdpClient();
        targetEndPoint = new IPEndPoint(IPAddress.Parse(pc_ip), dataPort);
        StartCommandListener(); // Start listening for Python commands immediately
>>>>>>> Stashed changes
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

<<<<<<< Updated upstream
    void LateUpdate()
    {
        // Handle the transition from Searching -> Streaming on the main thread
=======
    // --- COMMAND LISTENER (NEW) ---
    private void StartCommandListener()
    {
        try
        {
            commandListener = new UdpClient(commandPort);
            commandListener.BeginReceive(OnCommandReceived, null);
            Debug.Log($"[Commands] Listening for Python commands on port {commandPort}...");
        }
        catch (Exception e) { Debug.LogError($"[Commands] Error: {e.Message}"); }
    }

    private void OnCommandReceived(IAsyncResult res)
    {
        IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, commandPort);
        byte[] receivedBytes = commandListener.EndReceive(res, ref remoteEndpoint);
        string message = Encoding.UTF8.GetString(receivedBytes);

        // --- COMMAND PARSER ---
        if (message.StartsWith("SPEED:"))
        {
            string valueStr = message.Substring(6);
            string[] speeds = valueStr.Split("|");
            if (float.TryParse(speeds[0], out float leftVal))
            {
                currentFingerSpeedLeft = leftVal;
                leftHand.fingerRotationSpeed = leftVal;
                Debug.Log($"[Commands] Remote finger speed left updated to: {currentFingerSpeedLeft}");
            }

            if (float.TryParse(speeds[1], out float rightVal))
            {
                currentFingerSpeedRight = rightVal;
                rightHand.fingerRotationSpeed = rightVal;
                Debug.Log($"[Commands] Remote finger speed left updated to: {currentFingerSpeedRight}");
            }
        }
        else if (message == "CMD:RESET") // THE NEW RESET PARSER
        {
            Debug.Log("[Commands] Received Reset Command from Python!");
            triggerReset = true;
        }

        // Keep listening for the next command
        commandListener.BeginReceive(OnCommandReceived, null);
    }

    // --- DATA SENDING ---
    void LateUpdate()
    {
        // --- CHECK FOR MARBLE RESET ---
        if (triggerReset)
        {
            ResetMarbles();
            triggerReset = false; // Turn the switch off
        }
        
        /*
>>>>>>> Stashed changes
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
        */

        // Ensure skeletons are ready
        if (leftHandSkeleton == null || !leftHandSkeleton.IsInitialized ||
            rightHandSkeleton == null || !rightHandSkeleton.IsInitialized) 
            return;

<<<<<<< Updated upstream
        // Build and send the packet
        sb.Clear();
        sb.Append(Time.time).Append("|");
        AppendSkeletonData(leftHandSkeleton, "L");
        AppendSkeletonData(rightHandSkeleton, "R");

        byte[] data = Encoding.UTF8.GetBytes(sb.ToString());
        dataSender.Send(data, data.Length, targetEndPoint);
=======
        float currentFrameTime = Time.time;

        /*
        // --- PACKET 1: VISUAL HANDS ---
        sb.Clear();
        // NEW: We inject the currentFingerSpeed into the header!
        sb.Append(currentFrameTime).Append(",").Append(currentFingerSpeedLeft.ToString("F3")).Append("|");
        sb.Append(currentFrameTime).Append(currentFingerSpeedRight.ToString("F3")).Append("|");
        AppendSkeletonData(visualLeft, "VL");
        AppendSkeletonData(visualRight, "VR");
        byte[] visualData = Encoding.UTF8.GetBytes(sb.ToString());
        dataSender.Send(visualData, visualData.Length, targetEndPoint);

        // --- PACKET 2: REAL HANDS ---
        sb.Clear();
        // Ensure both packets share the exact same timestamp and speed state
        sb.Append(currentFrameTime).Append(",").Append(currentFingerSpeedLeft.ToString("F3")).Append("|");
        sb.Append(currentFrameTime).Append(currentFingerSpeedRight.ToString("F3")).Append("|");
        AppendSkeletonData(realLeft, "TL");
        AppendSkeletonData(realRight, "TR");
        byte[] realData = Encoding.UTF8.GetBytes(sb.ToString());
        dataSender.Send(realData, realData.Length, targetEndPoint);
        */

        // --- Send packet using current formating ---
        sb.Clear();
        sb.Append(currentFrameTime).Append("|");
        AppendSkeletonData(realLeft, "TL");
        AppendSkeletonData(realRight, "TR");
        AppendSkeletonData(visualLeft, "VL");
        AppendSkeletonData(visualRight, "VR");
>>>>>>> Stashed changes
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
        if (beaconListener != null) beaconListener.Close();
        if (dataSender != null) dataSender.Close();
    }
}