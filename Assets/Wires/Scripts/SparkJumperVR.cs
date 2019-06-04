using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Spark Jumper to be used on VR devices (Oculus Rift, Oculus Go)
    /// </summary>
    public class SparkJumperVR : SparkJumper
    {
        // Right hand controller is used for testing as the Go controller shares the same type
        public static OVRInput.Controller ControllerType = OVRInput.Controller.RTrackedRemote;

        void Update()
        {
            // Rotation is handled for us by the OVRCameraRig

            #if UNITY_EDITOR
            // Oculus Rift is used for testing in editor
            if (OVRInput.GetDown(OVRInput.Button.One))
                TraceSpark(OVRInput.GetLocalControllerPosition(ControllerType), OVRInput.GetLocalControllerRotation(ControllerType));
            #else
            // Oculus Go is used for packaged builds
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
                TraceSpark(OVRInput.GetLocalControllerPosition(ControllerType), OVRInput.GetLocalControllerRotation(ControllerType));
            #endif
        }

        void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Vector3 position = OVRInput.GetLocalControllerPosition(ControllerType);
                Quaternion rotation = OVRInput.GetLocalControllerRotation(ControllerType);

                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(position, 0.1f);
                Gizmos.DrawLine(position, position + (rotation * Vector3.forward) * 1000f);
            }
        }
    }
}
