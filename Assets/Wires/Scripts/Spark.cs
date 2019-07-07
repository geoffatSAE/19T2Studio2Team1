using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Sparks travel along wires and can be used as a host for the player
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class Spark : MonoBehaviour, IInteractive
    {
        // If player can jump to this spark
        public bool canJumpTo { get { return m_CanJumpTo; } }

        // Player on this spark
        public SparkJumper sparkJumper { get { return m_SparkJumper; } }

        // Sparks renderer
        public Renderer sparkRenderer { get { return m_Renderer; } }

        public float m_SwitchInterval = 2f;                                 // Interval for switching between on and off
        [Min(0.1f)] public float m_SwitchBlendDuration = 0.5f;              // Time for blending between on and off (visually)
        public Color m_OnColor = Color.yellow;                              // Color to use when on
        public Color m_OffColor = Color.red;                                // Color to use when off
        public Vector3 m_OnScale = Vector3.one;                             // Scale to use when on
        public Vector3 m_OffScale = new Vector3(0.5f, 0.5f, 0.5f);          // Scale to use when off
        [SerializeField] private Renderer m_Renderer;                       // Sparks renderer

        private Wire m_Wire;                        // Wire this spark is on
        private bool m_CanJumpTo = true;            // If player can jump to this spark
        private SparkJumper m_SparkJumper;          // Player on this spark
        private SphereCollider m_Collider;          // Collider for this spark
        private Coroutine m_SwitchRoutine;          // Switch coroutine that is running

        void Awake()
        {
            if (!m_Renderer)
                m_Renderer = GetComponentInChildren<Renderer>();

            m_Collider = GetComponent<SphereCollider>();
        }

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
                m_CanJumpTo = (Random.Range(0, 10) & 1) == 1;

                // Initially blend based on current status
                BlendSwitchStatus(m_CanJumpTo ? 1f : 0f);

                m_SwitchRoutine = StartCoroutine(SwitchRoutine());
            }
            else
            {
                m_CanJumpTo = true;
                BlendSwitchStatus(1f);

                m_SwitchRoutine = null;
            }

            m_Wire = wire;
            transform.position = wire.transform.position;
            m_Collider.enabled = m_CanJumpTo;
        }

        /// <summary>
        /// Deactivates this spark
        /// </summary>
        public void DeactivateSpark()
        {
            if (m_SwitchRoutine != null)
                StopCoroutine(m_SwitchRoutine);

            m_CanJumpTo = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Freezes the sparks jump state to true
        /// </summary>
        public void FreezeSwitching()
        {
            if (m_SwitchRoutine != null)
                StopCoroutine(m_SwitchRoutine);

            m_CanJumpTo = true;
            BlendSwitchStatus(1f);
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
                m_Collider.enabled = false;

                m_Wire.m_BorderMesh.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Detaches the jumper attached to this spark
        /// </summary>
        public void DetachJumper()
        {
            m_SparkJumper = null;
            BlendSwitchStatus(0f);

            m_Wire.m_BorderMesh.gameObject.SetActive(false);
        }

        /// <summary>
        /// Switch routine for enabled/disabling jumping
        /// </summary>
        private IEnumerator SwitchRoutine()
        {
            while (!m_SparkJumper)
            {
                yield return new WaitForSeconds(m_SwitchInterval);
                m_CanJumpTo = !m_CanJumpTo;
                m_Collider.enabled = m_CanJumpTo;

                // Blend between on and off
                {
                    float end = Time.time + m_SwitchBlendDuration;
                    while (Time.time < end)
                    {
                        float alpha = Mathf.Clamp01((end - Time.time) / m_SwitchBlendDuration);

                        // We negate to blend in opposite direction
                        if (m_CanJumpTo)
                            alpha = 1f - alpha;

                        BlendSwitchStatus(alpha);
                        yield return null;
                    }
                }
            }
        }

        private void BlendSwitchStatus(float progress)
        {
            if (m_Renderer)
                m_Renderer.material.color = Color.Lerp(m_OffColor, m_OnColor, progress);

            transform.localScale = Vector3.Lerp(m_OffScale, m_OnScale, progress);
        }

        /// <summary>
        /// Get the wire this spark is on
        /// </summary>
        /// <returns>Sparks wire</returns>
        public Wire GetWire()
        {
            return m_Wire;
        }

        // IInteractive Interface
        public bool CanInteract(SparkJumper jumper)
        {
            return m_CanJumpTo;
        }

        // IInteractive Interface
        public void OnInteract(SparkJumper jumper)
        {
            jumper.JumpToSpark(this);
        }
    }
}
