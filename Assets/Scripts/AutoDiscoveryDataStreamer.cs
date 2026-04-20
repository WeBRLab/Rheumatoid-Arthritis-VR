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
    public int commandPort = 5007; 
    private const string MAGIC_WORD = "VR_LOGGER_HERE";

    [Header("Simulation State")]
    [Tooltip("Read this value from your Arthritis script to apply the remote speed")]
    public float currentFingerSpeed = 8.0f; 

    [Header("Visual Hands (Arthritic)")]
    public OVRSkeleton visualLeft;
    public OVRSkeleton visualRight;

    [Header("Real Hands (Invisible/Tracked)")]
    public OVRSkeleton realLeft;
    public OVRSkeleton realRight;

    [Header("Hand Visuals (Meshes & Materials)")]
    [Tooltip("Drag the child object holding the SkinnedMeshRenderer for the LEFT hand here")]
    public SkinnedMeshRenderer leftHandRenderer;
    [Tooltip("Drag the child object holding the SkinnedMeshRenderer for the RIGHT hand here")]
    public SkinnedMeshRenderer rightHandRenderer;
    [Tooltip("Drop your 7 materials here (0-6)")]
    public Material[] availableHandMaterials;

    [Header("Marbles")]
    public List<Transform> marbles = new List<Transform>();
    
    private UdpClient beaconListener;
    private UdpClient commandListener; 
    private UdpClient dataSender;
    private IPEndPoint targetEndPoint;
    private StringBuilder sb;

    private bool isSearching = true;
    private bool isConnected = false;
    private string discoveredIP = "";

    // --- THREAD SAFETY SWITCHES ---
    private bool triggerReset = false; 
    private bool triggerHandChange = false;
    private int targetHandIndex = 0;

    void Start()
    {
        sb = new StringBuilder(2048);
        StartListeningForBeacon();
        StartCommandListener(); 
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

    // --- COMMAND LISTENER ---
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
        else if (message == "CMD:RESET") 
        {
            Debug.Log("[Commands] Received Reset Command from Python!");
            triggerReset = true; // Flip the switch for LateUpdate
        }
        else if (message.StartsWith("CMD:HAND:")) 
        {
            string valueStr = message.Substring(9); // Strip out "CMD:HAND:"
            if (int.TryParse(valueStr, out int handIndex))
            {
                Debug.Log($"[Commands] Received Hand Type Change Command: {handIndex}");
                targetHandIndex = handIndex; // Store the requested number
                triggerHandChange = true;    // Flip the switch for LateUpdate
            }
        }

        // Keep listening for the next command
        commandListener.BeginReceive(OnCommandReceived, null);
    }

    // --- DATA SENDING & MAIN THREAD EXECUTION ---
    void LateUpdate()
    {
        // 1. Safely execute resets on the main physics thread
        if (triggerReset)
        {
            ResetMarbles();
            triggerReset = false; 
        }

        // 2. Safely execute material swaps on the main rendering thread
        if (triggerHandChange)
        {
            ChangeHandMaterials(targetHandIndex);
            triggerHandChange = false;
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
        sb.Append(currentFrameTime).Append(",").Append(currentFingerSpeed.ToString("F3")).Append("|");
        AppendSkeletonData(visualLeft, "Visual", "L");
        AppendSkeletonData(visualRight, "Visual", "R");
        byte[] visualData = Encoding.UTF8.GetBytes(sb.ToString());
        dataSender.Send(visualData, visualData.Length, targetEndPoint);

        // --- PACKET 2: REAL HANDS ---
        sb.Clear();
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
    
    // --- EXECUTOR METHODS ---
    public void ResetMarbles()
    {
        foreach (Transform marble in marbles)
        {
            if (marble != null)
            {
                var marbleReset = marble.GetComponent<MarbleReset>();
                if (marbleReset != null) marbleReset.ResetPosition();
            }
        }
    }

    public void ChangeHandMaterials(int index)
    {
        if (availableHandMaterials == null || availableHandMaterials.Length == 0) return;

        if (index < 0 || index >= availableHandMaterials.Length)
        {
            Debug.LogWarning($"[Commands] Invalid hand index: {index}. You only have {availableHandMaterials.Length} materials assigned!");
            return;
        }
        
        rightHandRenderer.materials[0] = availableHandMaterials[index];
        rightHandRenderer.material = availableHandMaterials[index];
        leftHandRenderer.materials[0] = availableHandMaterials[index];
        leftHandRenderer.material = availableHandMaterials[index];
        
        Debug.Log($"[Commands] Successfully applied hand material index {index}");
    }

    void OnDestroy()
    {
        if (beaconListener != null) beaconListener.Close();
        if (commandListener != null) commandListener.Close();
        if (dataSender != null) dataSender.Close();
    }
}