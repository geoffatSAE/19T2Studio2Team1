using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Simple movement component that moves transform towards a position
    /// Enable component to move automatically (can be manually controlled while disabled)
    /// </summary>
    public class SeekMovement : MonoBehaviour
    {
        /// <summary>
        /// Delegate for when the chaser has reached its target
        /// </summary>
        public delegate void TargetReached();

        public Vector3 m_Target;        // Position to move to
        public float m_Speed = 2f;      // Time it takes to reach target

        public TargetReached OnTargetReached;       // Event for when the target has been reached initially
            
        private Vector3 m_Start;                // Position where we started seek
        private float m_StartTime;              // Time we started seeking
        private bool m_AtTarget = false;        // If we are at the target

        void Update()
        {
            Move(Time.deltaTime);   
        }

        /// <summary>
        /// Performs one step to move closer to target
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Move(float deltaTime)
        {
            if (m_AtTarget)
                return;

            Vector3 from = transform.position;

            // Move closer
            float alpha = Mathf.Clamp01((Time.time - m_StartTime) / m_Speed);
            transform.position = Vector3.Lerp(m_Start, m_Target, alpha);
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.4f, alpha);

            // Are we at target?
            if (alpha >= 1f)
            {
                m_AtTarget = true;
                if (OnTargetReached != null)
                    OnTargetReached.Invoke();
            }          
        }

        /// <summary>
        /// Sets the target to chase, restting if target has been reached
        /// </summary>
        /// <param name="target">Target to seek</param>
        public void SetTarget(Vector3 target)
        {
            m_Target = target;
            m_AtTarget = false;
            m_Start = transform.position;
            m_StartTime = Time.time;
        }
    }
}
