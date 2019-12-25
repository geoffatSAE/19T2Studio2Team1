using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Player controller for VR platforms, also supports in editor VR testing
    /// </summary>
    public class SparkJumperVR : SparkJumper
    {
        // Right hand controller is used for testing as the Go controller shares the same type
        public static OVRInput.Controller ControllerType = OVRInput.Controller.RTrackedRemote;

        public Transform m_ControllerOrigin;                        // Override transform for controllers origin
        public Transform m_HeadOrigin;                              // Origin of players head (headset)

        protected override void Update()
        {
            base.Update();

            // Rotation is handled for us by the OVRCameraRig

            Vector3 controllerPos = GetControllerPosition();
            Vector3 controllerDir = GetControllerRotation() * Vector3.forward;

            #if UNITY_EDITOR
            // Oculus Rift is used for testing in editor
            {
                if (OVRInput.GetDown(OVRInput.Button.One))
                    TraceWorld(new Ray(controllerPos, controllerDir));
            }
            #else
            // Oculus Go is used for packaged builds
            {
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
                    TraceWorld(new Ray(controllerPos, controllerDir));
            }
            #endif

            Shader.SetGlobalVector(WorldSpaceControllerPosShaderName, controllerPos);
            Shader.SetGlobalVector(WorldSpaceControllerDirShaderName, controllerDir);
        }

        /// <summary>
        /// Get the position of the players controller
        /// </summary>
        /// <returns>Position of controller in world space</returns>
        private Vector3 GetControllerPosition()
        {
            if (m_ControllerOrigin)
                return m_ControllerOrigin.TransformPoint(OVRInput.GetLocalControllerPosition(ControllerType));
            else
                return transform.TransformPoint(OVRInput.GetLocalControllerPosition(ControllerType));
        }

        /// <summary>
        /// Get the rotation of the players controller
        /// </summary>
        /// <returns>Rotation of controller in world space</returns>
        private Quaternion GetControllerRotation()
        {
            return OVRInput.GetLocalControllerRotation(ControllerType);
        }

        // SparkJumper Interface
        public override Vector3 GetPlayerPosition()
        {
            if (m_HeadOrigin)
                return m_HeadOrigin.position;

            return base.GetPlayerPosition();
        }

        public override void GetControllerPosAndDir(ref Vector3 OutPosition, ref Vector3 OutDirection)
        {
            OutPosition = GetControllerPosition();
            OutDirection = GetControllerRotation() * Vector3.forward;
        }
    }
}
