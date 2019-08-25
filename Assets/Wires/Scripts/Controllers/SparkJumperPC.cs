using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Player controller for PC, only used for testing in editor without a headset
    /// </summary>
    public class SparkJumperPC : SparkJumper
    {
        public float m_Sensitivity = 10f;           // Sensitivity of rotation
        public bool m_InvertPitch = false;          // If pitch (looking up and down) should be inverted
        public Camera m_Camera;                     // The camera used for tracing the world

        private float m_RotationX = 0f;             // Yaw rotation
        private float m_RotationY = 0f;             // Pitch rotation

        #if UNITY_EDITOR
        public LaserPointer m_LaserPointer;         // Testing laser pointer
        #endif

        void Awake()
        {
            if (!m_Camera)
                m_Camera = GetComponentInChildren<Camera>();
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        protected override void Update()
        {
            base.Update();

            // The transform to rotate and base tracing with
            Transform trans = m_Camera ? m_Camera.transform : transform;

            float turnX = Input.GetAxis("Mouse X");
            float turnY = Input.GetAxis("Mouse Y");

            if (turnX != 0f)
                m_RotationX += m_Sensitivity * turnX;

            if (turnY != 0f)
            {
                float delta = m_InvertPitch ? 1f : -1f * turnY * m_Sensitivity;
                m_RotationY = Mathf.Clamp(m_RotationY + delta, -85f, 85f);
            }

            trans.localEulerAngles = new Vector3(m_RotationY, m_RotationX, 0f);

            // Left mouse
            if (Input.GetMouseButtonDown(0))
                TraceWorld(trans.position, trans.rotation);

            // Right mouse
            if (Input.GetMouseButtonDown(1))
                ActivateBoost();

            Shader.SetGlobalVector(WorldSpaceControllerPosShaderName, trans.position);
            Shader.SetGlobalVector(WorldSpaceControllerDirShaderName, trans.forward);

            #if UNITY_EDITOR
            if (m_LaserPointer)
                m_LaserPointer.PointLaser(trans.position, trans.forward);
            
            // Pause in editor
            if (Input.GetKeyDown(KeyCode.B))
                Debug.Break();
            #endif
        }

        // SparkJumper Interface
        public override Vector3 GetPlayerPosition()
        {
            if (m_Camera)
                return m_Camera.transform.position;

            return base.GetPlayerPosition();
        }
    }
}
