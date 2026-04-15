using System;
using System.Collections.Generic;
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
    public int commandPort = 5007; // NEW: Port to receive commands from Python
    private const string MAGIC_WORD = "VR_LOGGER_HERE";

    [Header("Simulation State")]
    [Tooltip("Read this value from your Arthritis script to apply the remote speed")]
    public float currentFingerSpeed = 8.0f; // Python will remotely change this!

    [Header("Visual Hands (Arthritic)")]
    public OVRSkeleton visualLeft;
    public OVRSkeleton visualRight;

    [Header("Real Hands (Invisible/Tracked)")]
    public OVRSkeleton realLeft;
    public OVRSkeleton realRight;

    [Header("Marbles")]
    public List<Transform> marbles = new List<Transform>();
    
    private UdpClient beaconListener;
    private UdpClient commandListener; // NEW: Listens for parameter changes
    private UdpClient dataSender;
    private IPEndPoint targetEndPoint;
    private StringBuilder sb;

    private bool isSearching = true;
    private bool isConnected = false;
    private string discoveredIP = "";

    private bool triggerReset = false; //used to trigger a marble reset

    void Start()
    {
        sb = new StringBuilder(2048);
        StartListeningForBeacon();
        StartCommandListener(); // Start listening for Python commands immediately
    }

    // --- BEACON DISCOVERY ---
    private void StartListeningForBeacon()
    {
        try
        {
            beaconListener = new UdpClient(beaconPort);
            beaconListener.BeginReceive(OnBeaconReceived, null);
        }
        catch (Exception e) { Debug.LogError($"[Beacon] Error: {e.Message}"); }
    }

    private void OnBeaconReceived(IAsyncResult res)
    {
        if (!isSearching) return;

        IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, beaconPort);
        byte[] receivedBytes = beaconListener.EndReceive(res, ref remoteEndpoint);
        string message = Encoding.UTF8.GetString(receivedBytes);

        if (message == MAGIC_WORD)
        {
            discoveredIP = remoteEndpoint.Address.ToString();
            Debug.Log($"[Auto-Discovery] Found Logger at {discoveredIP}");
            isSearching = false;
            isConnected = true;
        }
        else
        {
            beaconListener.BeginReceive(OnBeaconReceived, null);
        }
    }

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
            if (float.TryParse(valueStr, out float newVal))
            {
                currentFingerSpeed = newVal;
                Debug.Log($"[Commands] Remote finger speed updated to: {currentFingerSpeed}");
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
        
        if (isConnected && dataSender == null)
        {
            dataSender = new UdpClient();
            targetEndPoint = new IPEndPoint(IPAddress.Parse(discoveredIP), dataPort);
            if (beaconListener != null) { beaconListener.Close(); beaconListener = null; }
        }

        if (!isConnected || dataSender == null) return;

        if (visualLeft is null || !visualLeft.IsInitialized ||
            visualRight is null || !visualRight.IsInitialized ||
            realLeft is null || !realLeft.IsInitialized ||
            realRight is null || !realRight.IsInitialized) 
            return;

        float currentFrameTime = Time.time;

        // --- PACKET 1: VISUAL HANDS ---
        sb.Clear();
        // NEW: We inject the currentFingerSpeed into the header!
        sb.Append(currentFrameTime).Append(",").Append(currentFingerSpeed.ToString("F3")).Append("|");
        AppendSkeletonData(visualLeft, "Visual", "L");
        AppendSkeletonData(visualRight, "Visual", "R");
        byte[] visualData = Encoding.UTF8.GetBytes(sb.ToString());
        dataSender.Send(visualData, visualData.Length, targetEndPoint);

        // --- PACKET 2: REAL HANDS ---
        sb.Clear();
        // Ensure both packets share the exact same timestamp and speed state
        sb.Append(currentFrameTime).Append(",").Append(currentFingerSpeed.ToString("F3")).Append("|");
        AppendSkeletonData(realLeft, "Real", "L");
        AppendSkeletonData(realRight, "Real", "R");
        byte[] realData = Encoding.UTF8.GetBytes(sb.ToString());
        dataSender.Send(realData, realData.Length, targetEndPoint);
    }

    private void AppendSkeletonData(OVRSkeleton skeleton, string handType, string handSide)
    {
        foreach (var bone in skeleton.Bones)
        {
            Transform t = bone.Transform;
            sb.Append(handType).Append(",")
              .Append(handSide).Append(",")
              .Append((int)bone.Id).Append(",")
              .Append(t.position.x.ToString("F4")).Append(",")
              .Append(t.position.y.ToString("F4")).Append(",")
              .Append(t.position.z.ToString("F4")).Append(",")
              .Append(t.rotation.x.ToString("F4")).Append(",")
              .Append(t.rotation.y.ToString("F4")).Append(",")
              .Append(t.rotation.z.ToString("F4")).Append(",")
              .Append(t.rotation.w.ToString("F4")).Append("|");
        }
    }
    
    // --- RESET MARBLES ---
    public void ResetMarbles()
    {
        foreach (Transform marble in marbles)
        {
            marble.GetComponent<MarbleReset>().ResetPosition();
        }
    }

    void OnDestroy()
    {
        if (beaconListener != null) beaconListener.Close();
        if (commandListener != null) commandListener.Close();
        if (dataSender != null) dataSender.Close();
    }
}