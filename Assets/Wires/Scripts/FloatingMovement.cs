using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{ 
    /// <summary>
    /// Simple movement component that moves transform up and down (in Local space)
    /// </summary>
    public class FloatingMovement : MonoBehaviour
    {
        public float m_Speed = 2f;          // Speed of bobbing
        public float m_Offset = 1f;         // Offset from origin to float to

        void Update()
        {
            Vector3 position = transform.localPosition;
            position.y = m_Offset * Mathf.Sin(Time.time * m_Speed);
            transform.localPosition = position;
        }
    }
}
