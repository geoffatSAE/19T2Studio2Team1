﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Player controller for VR platforms, also supports in editor VR testing
    /// </summary>
    public class SparkJumperVRAlt : SparkJumperAlt
    {
        // Right hand controller is used for testing as the Go controller shares the same type
        public static OVRInput.Controller ControllerType = OVRInput.Controller.RTrackedRemote;

        public Transform m_ControllerOrigin;        // Override transform for controllers origin

        void Update()
        {
            // Rotation is handled for us by the OVRCameraRig

            #if UNITY_EDITOR
            // Oculus Rift is used for testing in editor
            if (OVRInput.GetDown(OVRInput.Button.One))
                TraceSpark(GetControllerPosition(), OVRInput.GetLocalControllerRotation(ControllerType));
            #else
            // Oculus Go is used for packaged builds
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
                TraceSpark(GetControllerPosition(), OVRInput.GetLocalControllerRotation(ControllerType));
            #endif
        }

        private Vector3 GetControllerPosition()
        {
            if (m_ControllerOrigin)
                return m_ControllerOrigin.TransformPoint(OVRInput.GetLocalControllerPosition(ControllerType));
            else
                return transform.TransformPoint(OVRInput.GetLocalControllerPosition(ControllerType));
        }
    }
}
