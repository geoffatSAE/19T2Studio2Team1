﻿using System.Collections;
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

        public float m_OnSwitchInterval = 2f;                               // Interval spark will remain on
        public float m_OffSwitchInterval = 0.5f;                            // Interval spark will remain off
        [Min(0.1f)] public float m_SwitchBlendDuration = 0.5f;              // Time for blending between on and off (visually)
        public Color m_DeactivatedColor = Color.red;                        // Color to use when we can't be jumped to
        public Vector3 m_OnScale = Vector3.one;                             // Scale to use when on
        public Vector3 m_OffScale = new Vector3(0.5f, 0.5f, 0.5f);          // Scale to use when off
        public bool m_Rotate = true;                                        // If spark should rotate
        [Min(0.1f)] public float m_RotationTime = 0.5f;                     // Time it takes for spark to rotate   
        [SerializeField] private Renderer m_Renderer;                       // Sparks renderer
        [SerializeField] private ParticleSystem[] m_Particles;              // Sparks particles
        public AudioClip m_OnSelectedSound;                                 // Sound to play when selected while on
        public AudioClip m_OffSelectedSound;                                // Sound to play when selected while off

        private Wire m_Wire;                                        // Wire this spark is on
        private bool m_CanJumpTo = true;                            // If player can jump to this spark
        private Vector3 m_OnTargetScale;                            // Scale spark should be at when on
        private bool m_IsSwitching = false;                         // If spark is switching states
        private SparkJumper m_SparkJumper;                          // Player on this spark
        private SphereCollider m_Collider;                          // Collider for this spark
        private Coroutine m_SwitchRoutine;                          // Switch coroutine that is running
        private float m_RotateEnd = -1f;                            // Time rotation sequence should finish
        private Quaternion m_RotateFrom = Quaternion.identity;      // Rotation to rotate from
        private Quaternion m_RotateTo = Quaternion.identity;        // Rotation to rotate to

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
        public void ActivateSpark(Wire wire, float onInterval, float offInterval)
        {
            gameObject.SetActive(true);

            m_OnSwitchInterval = onInterval;
            m_OffSwitchInterval = offInterval;

            // Start switch routine only if interval is set
            if (m_OnSwitchInterval > 0f && m_OffSwitchInterval > 0f)
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

                m_IsSwitching = false;
                m_SwitchRoutine = null;
            }

            m_Wire = wire;
            transform.position = wire.transform.position;
            m_OnTargetScale = m_OnScale;
            m_Collider.enabled = true;// m_CanJumpTo;

            if (m_Wire.factory)
                SetSparkColor(m_Wire.factory.sparkColor);
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
        /// Tick this spark, updating aesthetic details
        /// </summary>
        /// <param name="step">Step this frame</param>
        /// <param name="progress">Progress along wire</param>
        public void TickSpark(float step, float progress)
        {
            if (m_Rotate && m_Renderer)
            {
                if (m_RotateEnd < 0f)
                {
                    m_RotateFrom = m_Renderer.transform.rotation;
                    m_RotateTo = Random.rotation;
                    m_RotateEnd = Time.time + m_RotationTime;
                }

                // We reverse target and from as alpha is also reversed
                float alpha = Mathf.Clamp01((m_RotateEnd - Time.time) / m_RotationTime);
                m_Renderer.transform.rotation = Quaternion.Slerp(m_RotateTo, m_RotateFrom, alpha);

                if (Time.time > m_RotateEnd)
                    m_RotateEnd = -1f;
            }

            // InQuint easing function
            // See https://easings.net/en
            float ease = progress * progress * progress * progress * progress;

            m_OnTargetScale = Vector3.Lerp(m_OnScale, m_OffScale, ease);

            // We don't want to override defective spark
            if (!m_IsSwitching && (canJumpTo || m_SparkJumper != null))
                transform.localScale = m_OnTargetScale;
        }

        /// <summary>
        /// Freezes the sparks jump state to true
        /// </summary>
        public void FreezeSwitching()
        {
            if (m_SwitchRoutine != null)
            {
                StopCoroutine(m_SwitchRoutine);
                m_SwitchRoutine = null;
            }

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

                //SetSparkColor(m_PossessedColor);
            }
        }

        /// <summary>
        /// Detaches the jumper attached to this spark
        /// </summary>
        public void DetachJumper()
        {
            m_SparkJumper = null;
            BlendSwitchStatus(0f);
        }

        /// <summary>
        /// Switch routine for enabled/disabling jumping
        /// </summary>
        private IEnumerator SwitchRoutine()
        {
            while (!m_SparkJumper)
            {
                m_IsSwitching = false;
                yield return new WaitForSeconds(m_CanJumpTo ? m_OnSwitchInterval : m_OffSwitchInterval);
                m_IsSwitching = true;

                m_CanJumpTo = !m_CanJumpTo;
                //m_Collider.enabled = m_CanJumpTo;               

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

        /// <summary>
        /// Blends the sparks color and size based on progress
        /// </summary>
        /// <param name="progress">Progress of blend</param>
        private void BlendSwitchStatus(float progress)
        {
            if (m_Wire && m_Wire.factory)
            {
                Color color = Color.Lerp(m_DeactivatedColor, m_Wire.factory.sparkColor, progress);
                SetSparkColor(color);
            }

            transform.localScale = Vector3.Lerp(m_OffScale, m_OnTargetScale, progress);
        }

        /// <summary>
        /// Sets the sparks color, including its mesh and particles
        /// </summary>
        /// <param name="color">Color of spark</param>
        public void SetSparkColor(Color color)
        {
            if (m_Renderer)
                m_Renderer.material.color = color;

            foreach (ParticleSystem system in m_Particles)
            {
                if (!system)
                    continue;

                ParticleSystem.ColorOverLifetimeModule colorModule = system.colorOverLifetime;
                colorModule.color = color;
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

        // IInteractive Interface
        public bool CanInteract(SparkJumper jumper)
        {
            return true;
        }

        // IInteractive Interface
        public void OnInteract(SparkJumper jumper)
        {
            if (m_CanJumpTo)
            {
                jumper.JumpToSpark(this);
                jumper.PlaySelectionSound(m_OnSelectedSound);
            }
            else
            {
                jumper.PlaySelectionSound(m_OffSelectedSound);
            }
        }
    }
}
