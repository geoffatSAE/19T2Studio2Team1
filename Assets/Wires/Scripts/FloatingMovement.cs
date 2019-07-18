using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{ 
    /// <summary>
    /// Simple movement component that moves transform up and down (in Local space)
    /// Enable component to move automatically (can be manually controlled while disabled)
    /// </summary>
    public class FloatingMovement : MonoBehaviour
    {
        public float m_Speed = 2f;          // Speed of bobbing
        public float m_Offset = 1f;         // Offset from origin to float to

        private float m_Time = 0f;          // Time we have been moving for

        void Update()
        {
            Move(Time.deltaTime);
        }

        /// <summary>
        /// Floats by progressing time forward
        /// </summary>
        /// <param name="deltaTime">Time to move by</param>
        public void Move(float deltaTime)
        {
            m_Time += m_Speed * deltaTime;

            Vector3 position = transform.localPosition;
            position.y = m_Offset * Mathf.Sin(m_Time);
            transform.localPosition = position;
        }
    }
}
