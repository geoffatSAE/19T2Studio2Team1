using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    [RequireComponent(typeof(SphereCollider))]
    public class Spark : MonoBehaviour
    {
        // If player can jump to this spark
        public bool canJumpTo { get { return m_CanJumpTo; } }

        // Player on this spark
        public SparkJumper sparkJumper { get { return m_SparkJumper; } }

        public float m_SwitchInterval = 2f;         // Interval for switching between on and off

        private Wire m_Wire;                        // Wire this spark is on
        private bool m_CanJumpTo = true;            // If player can jump to this spark
        private SparkJumper m_SparkJumper;          // Player on this spark

        [SerializeField] private Material m_ActiveMaterial;
        [SerializeField] private Material m_OccupiedMaterial;

        /// <summary>
        /// Activates this spark
        /// </summary>
        /// <param name="wire">Wire we are attached to</param>
        /// <param name="interval">Interval for switching jump states</param>
        public void ActivateSpark(Wire wire, float interval)
        {
            gameObject.SetActive(true);

            m_SwitchInterval = interval;

            // Start switch routine only if interval is set
            if (interval > 0f)
            {
                //m_CanJumpTo = (Random.Range(0, 10) & 1) == 1;
                //StartCoroutine(SwitchRoutine());
                m_CanJumpTo = true;
            }
            else
            {
                m_CanJumpTo = true;
            }

            m_Wire = wire;
            transform.position = wire.transform.position;

            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer)
                renderer.material = m_ActiveMaterial;

            SphereCollider collider = GetComponent<SphereCollider>();
            if (collider)
                collider.enabled = true;
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
        public void AttachJumper(SparkJumper jumper)
        {
            if (canJumpTo)
            {
                m_SparkJumper = jumper;
                m_CanJumpTo = false;

                Renderer renderer = GetComponentInChildren<Renderer>();
                if (renderer)
                    renderer.material = m_OccupiedMaterial;

                m_Wire.m_BorderMesh.gameObject.SetActive(true);

                SphereCollider collider = GetComponent<SphereCollider>();
                if (collider)
                    collider.enabled = false;
            }
        }

        /// <summary>
        /// Detaches the jumper attached to this spark
        /// </summary>
        public void DetachJumper()
        {
            m_SparkJumper = null;

            m_Wire.m_BorderMesh.gameObject.SetActive(false);
        }

        /// <summary>
        /// Switch routine for enabled/disabling jumping
        /// </summary>
        private IEnumerator SwitchRoutine()
        {
            while (enabled)
            {
                yield return new WaitForSeconds(m_SwitchInterval);
                //m_CanJumpTo = true;// !m_CanJumpTo;
            }
        }

        /// <summary>
        /// Get the wire this spark is on
        /// </summary>
        /// <returns>Sparks wire</returns>
        public Wire GetWire()
        {
            return m_Wire;
        }

        void OnDrawGizmos()
        {
            SphereCollider collider = GetComponent<SphereCollider>();

            Gizmos.color = m_CanJumpTo ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, collider.radius);
        }
    }
}
