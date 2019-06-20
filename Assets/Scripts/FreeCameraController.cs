using UnityEngine;

namespace TO5
{
    /// <summary>
    /// Simple script for allowing free movement in a scene
    /// </summary>
    public class FreeCameraController : MonoBehaviour
    {
        public float m_Speed = 5f;          // Speed per second
        public float m_Sensitivity = 10f;   // Speed at which to rotate

        private float m_RotationX;      // Yaw rotation
        private float m_RotationY;      // Pitch rotation

        void Start()
        {
            m_RotationX = 0f;
            m_RotationY = 0f;
        }

        void Update()
        {
            float forward = Input.GetAxis("Vertical");
            float right = Input.GetAxis("Horizontal");

            Vector3 velocity = Vector3.zero;
            velocity += transform.forward * forward * m_Speed * Time.deltaTime;
            velocity += transform.right * right * m_Speed * Time.deltaTime;

            transform.position += velocity;

            float turnX = Input.GetAxis("Mouse X");
            float turnY = Input.GetAxis("Mouse Y");

            if (turnX != 0f)
                m_RotationX += m_Sensitivity * turnX;

            if (turnY != 0f)
            {
                float delta = -1f * turnY * m_Sensitivity;
                m_RotationY = Mathf.Clamp(m_RotationY + delta, -85f, 85f);
            }

            transform.eulerAngles = new Vector3(m_RotationY, m_RotationX, 0f);
        }
    }
}
