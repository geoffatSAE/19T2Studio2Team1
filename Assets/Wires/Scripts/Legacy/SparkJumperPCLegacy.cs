﻿using System;
using UnityEngine;

namespace TO5.Wires.Legacy
{
    /// <summary>
    /// Spark Jumper to be used on PC devices (any device with a mouse or controller)
    /// </summary>
    [Obsolete]
    public class SparkJumperPCLegacy : SparkJumperLegacy
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
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
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

            if (Input.GetMouseButtonDown(0))
            {
                // Use mouse position if mouse happens to be present
                if (m_Camera)
                    TraceSpark(m_Camera.transform.position, m_Camera.transform.rotation);
                else
                    TraceSpark(trans.position, trans.rotation);
            }
        }
    }
}
