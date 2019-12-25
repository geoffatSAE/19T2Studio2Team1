using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Player controller for keyboard and mouse controls
    /// </summary>
    public class SparkJumperPC : SparkJumper
    {
        public float m_Sensitivity = 10f;           // Sensitivity of rotation
        public bool m_InvertPitch = false;          // If pitch (looking up and down) should be inverted
        public Camera m_Camera;                     // The camera used for tracing the world

        private float m_RotationX = 0f;             // Yaw rotation
        private float m_RotationY = 0f;             // Pitch rotation

        void Awake()
        {
            if (!m_Camera)
                m_Camera = GetComponentInChildren<Camera>();

            if (!m_Camera)
                Debug.LogError("SparkJumperPC expects a camera to be provided!");  
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        protected override void Update()
        {
            base.Update();

            float turnX = Input.GetAxis("Mouse X");
            float turnY = Input.GetAxis("Mouse Y");

            if (turnX != 0f)
                m_RotationX += m_Sensitivity * turnX;

            if (turnY != 0f)
            {
                float delta = m_InvertPitch ? 1f : -1f * turnY * m_Sensitivity;
                m_RotationY = Mathf.Clamp(m_RotationY + delta, -85f, 85f);
            }

            // New rotation in local euler space
            Vector3 newRotation = new Vector3(m_RotationY, m_RotationX, 0f);

            // Consider no camera being provided
            Ray ray = new Ray();
            if (m_Camera)
            {
                m_Camera.transform.localEulerAngles = newRotation;
                ray = m_Camera.ScreenPointToRay(Input.mousePosition);
            }
            else
            {
                transform.localEulerAngles = newRotation;
                ray = new Ray(transform.position, transform.forward);
            }

            // Left mouse
            if (Input.GetMouseButtonDown(0))
                TraceWorld(ray);

            Shader.SetGlobalVector(WorldSpaceControllerPosShaderName, ray.origin);
            Shader.SetGlobalVector(WorldSpaceControllerDirShaderName, ray.direction);

#if UNITY_EDITOR
            // Pause in editor
            if (Input.GetKeyDown(KeyCode.B))
                Debug.Break();
#endif
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            // Not optimal, but we need to at least wait one update
            // This also causes jitterness mouse is moving
            LaserPointer2D pointer2D = m_LaserPointer as LaserPointer2D;
            if (pointer2D)
                pointer2D.SetCrosshairPosition(Input.mousePosition);
        }

        // SparkJumper Interface
        public override Vector3 GetPlayerPosition()
        {
            if (m_Camera)
                return m_Camera.transform.position;

            return base.GetPlayerPosition();
        }

        public override void GetControllerPosAndDir(ref Vector3 OutPosition, ref Vector3 OutDirection)
        {
            if (m_Camera)
            {
                Ray ray = m_Camera.ScreenPointToRay(Input.mousePosition);

                OutPosition = ray.origin;
                OutDirection = ray.direction;
            }
            else
            {
                OutPosition = transform.position;
                OutDirection = transform.forward;
            }
        }
    }
}
