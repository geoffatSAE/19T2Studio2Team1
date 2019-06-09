using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Combat
{
    /// <summary>
    /// Controller to be used on PC platforms (platforms with gamepad support)
    /// </summary>
    public class CombatControllerPC : CombatController
    {
        public float m_Sensitivity = 10f;           // Sensitivity of rotation
        public bool m_InvertPitch = false;          // If pitch (looking up and down) should be inverted

        private float m_RotationX = 0f;             // Local yaw rotation
        private float m_RotationY = 0f;             // Local pitch rotation

        protected override void Start()
        {
            base.Start();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            float turnX = Input.GetAxis("Mouse X");
            float turnY = Input.GetAxis("Mouse Y");

            if (turnX != 0f)
                m_RotationX += m_Sensitivity * turnX;

            if (turnY != 0f)
            {
                float delta = m_InvertPitch ? 1f : -1f * m_Sensitivity * turnY;
                m_RotationY = Mathf.Clamp(m_RotationY + delta, -85f, 85f);
            }

            transform.localRotation = Quaternion.Euler(m_RotationY, m_RotationX, 0f);

            if (Input.GetMouseButtonDown(0))
                Fire(transform.position, transform.rotation);
        }
    }
}
