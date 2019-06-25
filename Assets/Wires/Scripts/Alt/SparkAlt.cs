using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    [RequireComponent(typeof(SphereCollider))]
    public class SparkAlt : MonoBehaviour
    {
        // If player can jump to this spark
        public bool canJumpTo { get { return m_CanJumpTo; } }

        // Player on this spark
        public SparkJumperAlt sparkJumper { get { return m_SparkJumper; } }

        public float m_SwitchInterval = 2f;         // Interval for switching between on and off

        private WireAlt m_Wire;                     // Wire this spark is on
        private bool m_CanJumpTo = true;            // If player can jump to this spark
        private SparkJumperAlt m_SparkJumper;       // Player on this spark

        /// <summary>
        /// Activates this spark
        /// </summary>
        /// <param name="wire">Wire we are attached to</param>
        /// <param name="interval">Interval for switching jump states</param>
        public void ActivateSpark(WireAlt wire, float interval)
        {
            gameObject.SetActive(true);

            m_SwitchInterval = interval;

            // Start switch routine only if interval is set
            if (interval > 0f)
            {
                m_CanJumpTo = (Random.Range(0, 10) & 1) == 1;
                StartCoroutine(SwitchRoutine());
            }
            else
            {
                m_CanJumpTo = true;
            }

            m_Wire = wire;
            transform.position = wire.transform.position;      
        }

        /// <summary>
        /// Deactivates this spark
        /// </summary>
        public void DeactivateSpark()
        {
            StopCoroutine(SwitchRoutine());
            m_CanJumpTo = false;

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Freezes the sparks jump state to true
        /// </summary>
        public void FreezeSwitching()
        {
            StopCoroutine(SwitchRoutine());
            m_CanJumpTo = true;       
        }

        /// <summary>
        /// Attaches the jumper to this spark
        /// </summary>
        /// <param name="jumper">Jumper to attach</param>
        public void AttachJumper(SparkJumperAlt jumper)
        {
            if (canJumpTo)
            {
                m_SparkJumper = jumper;
                m_CanJumpTo = false;
            }
        }

        /// <summary>
        /// Detaches the jumper attached to this spark
        /// </summary>
        public void DetachJumper()
        {
            m_SparkJumper = null;
        }

        /// <summary>
        /// Switch routine for enabled/disabling jumping
        /// </summary>
        private IEnumerator SwitchRoutine()
        {
            while (enabled)
            {
                yield return new WaitForSeconds(m_SwitchInterval);
                m_CanJumpTo = !m_CanJumpTo;
            }
        }

        /// <summary>
        /// Get the wire this spark is on
        /// </summary>
        /// <returns>Sparks wire</returns>
        public WireAlt GetWire()
        {
            return m_Wire;
        }

        void OnDrawGizmos()
        {
            SphereCollider collider = GetComponent<SphereCollider>();

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, collider.radius);
        }
    }
}
