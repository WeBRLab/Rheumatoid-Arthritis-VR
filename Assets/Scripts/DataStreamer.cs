using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(200)]
public class DataStreamer : MonoBehaviour
{
    [Header("Network Data")]
    public string pc_ip = "192.168.0.100";
    public int port = 5005;

    [Header("Hand References")]
    public OVRSkeleton leftHandSkeletonVisual;
    public OVRSkeleton rightHandSkeletonVisual;
    public OVRSkeleton leftHandSkeletonTarget;
    public OVRSkeleton rightHandSkeletonTarget;

    private UdpClient dataSender;
    private IPEndPoint targetEndPoint;
    private StringBuilder sb;

    void Start()
    {
        sb = new StringBuilder(2048);
        dataSender = new UdpClient();
        targetEndPoint = new IPEndPoint(IPAddress.Parse(pc_ip), port);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        // Ensure skeletons are ready
        if (leftHandSkeletonVisual == null || !leftHandSkeletonVisual.IsInitialized ||
            rightHandSkeletonVisual == null || !rightHandSkeletonVisual.IsInitialized ||
            leftHandSkeletonTarget == null || !leftHandSkeletonTarget.IsInitialized ||
            rightHandSkeletonTarget == null || !rightHandSkeletonTarget.IsInitialized)
            return;

        // Build and send the packet
        sb.Clear();
        sb.Append(Time.time).Append("|");
        AppendSkeletonData(leftHandSkeletonTarget, "TL");
        AppendSkeletonData(rightHandSkeletonTarget, "TR");
        AppendSkeletonData(leftHandSkeletonVisual, "VL");
        AppendSkeletonData(rightHandSkeletonVisual, "VR");

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
        if (dataSender != null) dataSender.Close();
    }
}
