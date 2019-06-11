using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Combat
{
    /// <summary>
    /// Controller to be used on VR platforms
    /// </summary>
    public class CombatControllerVR : CombatController
    {
        public static OVRInput.Controller ControllerType = OVRInput.Controller.RTrackedRemote;

        void Update()
        {
            // Rotation is handled for us by the OVRCameraRig

            #if UNITY_EDITOR
            // Oculus Rift is used for testing in editor
            if (OVRInput.GetDown(OVRInput.Button.One))
                Fire(transform.TransformPoint(OVRInput.GetLocalControllerPosition(ControllerType)), OVRInput.GetLocalControllerRotation(ControllerType));
            #else
             if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
                Fire(transform.TransformPoint(OVRInput.GetLocalControllerPosition(ControllerType)), OVRInput.GetLocalControllerRotation(ControllerType));
            #endif
        }
    }
}
