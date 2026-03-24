using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)] // Ensures this runs AFTER Meta updates the skeletons
public class ArthritisHandVisualizer : MonoBehaviour
{
    [Header("Skeletons")]
    [Tooltip("The INVISIBLE hand that tracks 1:1 with reality.")]
    public OVRSkeleton targetSkeleton;

    [Tooltip("The VISIBLE hand that this script is attached to.")]
    public OVRSkeleton visualSkeleton;

    [Header("Stiffness")]
    [Tooltip("Lower values mean stiffer fingers. Higher values mean closer to 1:1 movement.")]
    public float fingerRotationSpeed = 8f;

    // We MUST store the damped state independently, because Meta overwrites the visual bones every frame
    private Quaternion[] dampedRotations;
    private bool isInitialized = false;

    void Start()
    {
        // Automatically grab the skeleton on this object if not assigned
        if (visualSkeleton == null) visualSkeleton = GetComponent<OVRSkeleton>();
    }

    void LateUpdate()
    {
        if (!targetSkeleton.IsInitialized || !visualSkeleton.IsInitialized) return;

        if (!isInitialized)
        {
            InitializeDampedState();
            isInitialized = true;
        }

        ApplyDampedRotations();
    }

    private void InitializeDampedState()
    {
        IList<OVRBone> visualBones = visualSkeleton.Bones;
        dampedRotations = new Quaternion[visualBones.Count];

        for (int i = 0; i < visualBones.Count; i++)
        {
            dampedRotations[i] = visualBones[i].Transform.localRotation;
        }
    }

    private void ApplyDampedRotations()
    {
        IList<OVRBone> targetBones = targetSkeleton.Bones;
        IList<OVRBone> visualBones = visualSkeleton.Bones;

        // Failsafe in case something goes horribly wrong with the SDK
        if (targetBones.Count != visualBones.Count) return;

        for (int i = 0; i < visualBones.Count; i++)
        {
            // Index 0 and 1 are usually Wrist and Forearm. Track perfectly 1:1 with NO lag.
            if (i <= 1)
            {
                dampedRotations[i] = targetBones[i].Transform.localRotation;
                visualBones[i].Transform.localRotation = targetBones[i].Transform.localRotation;
                visualBones[i].Transform.localPosition = targetBones[i].Transform.localPosition;
            }
            else // Only the fingers get the heavy arthritis lag
            {
                // Calculate the new damped rotation and store it
                dampedRotations[i] = Quaternion.Slerp(
                    dampedRotations[i], 
                    targetBones[i].Transform.localRotation, 
                    Time.deltaTime * fingerRotationSpeed
                );

                // OVERWRITE the visual skeleton's finger bone with our lagged rotation
                visualBones[i].Transform.localRotation = dampedRotations[i];
            }
        }
    }
}