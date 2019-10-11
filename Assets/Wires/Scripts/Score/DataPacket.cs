using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Controller for data packets the player can collect
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class DataPacket : MonoBehaviour, IInteractive
    {
        /// <summary>
        /// Delegate for when data packet has either expired or been collected by the player
        /// </summary>
        /// <param name="packet">Packet that has been collected</param>
        public delegate void PacketCollected(DataPacket packet);

        public PacketCollected OnCollected;     // Event for when player collects this packet
        public PacketCollected OnSeekComplete;  // Event for when packet has finished seeking its target
        public PacketCollected OnExpired;       // Event for when this packet expires

        [SerializeField] private Renderer m_Renderer;                       // Packets mesh renderer
        [SerializeField] private TrailRenderer m_Trails;                    // Packets trails when seeking
        [SerializeField] private FloatingMovement m_FloatingMovement;       // Float movement component (used when collected)
        [SerializeField] private SeekMovement m_SeekMovement;               // Seek movement component (used when collected)
        [SerializeField] private Animator m_Animator;                       // Packets animator
        public AudioClip m_SelectedSound;                                   // Sound to play when selected

        private float m_Speed = 0f;                 // The speed of this packet
        private bool m_Seek = false;                // If this packet should seek target

        void Awake()
        {
            if (m_FloatingMovement)
                m_FloatingMovement.enabled = false;

            if (m_SeekMovement)
            {
                m_SeekMovement.enabled = false;
                m_SeekMovement.OnTargetReached += TargetReached;
            }

            if (m_Trails)
                m_Trails.enabled = false;
        }

        /// <summary>
        /// Activates this data packet
        /// </summary>
        /// <param name="position">Position of packet</param>
        /// <param name="speed">Speed of packet</param>
        /// <param name="lifetime">Lifetime before expiration</param>
        public void Activate(Vector3 position, float speed, float lifetime)
        {
            gameObject.SetActive(true);

            transform.position = position;
            transform.localScale = Vector3.one;
            m_Speed = speed;
            m_Seek = false;

            if (m_SeekMovement)
                m_SeekMovement.m_Target = null;

            if (m_Renderer)
                m_Renderer.enabled = true;

            if (m_Trails)
                m_Trails.enabled = false;

            Invoke("Expire", lifetime);
        }

        /// <summary>
        /// Deactivates this data packet
        /// </summary>
        public void Deactivate()
        {
            CancelInvoke();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Ticks this packet, moving the packet backwards
        /// </summary>
        /// <param name="step">Amount to move packet by (scaled by speed)</param>
        public void TickPacket(float step)
        {
            // We let seek movement control us if collected
            if (m_Seek)
            {
                if (m_SeekMovement)
                    m_SeekMovement.Move(Time.deltaTime);
            }
            else
            {
                // We move in the opposite direction of sparks (so we subtract)
                step *= m_Speed;
                transform.position -= WireManager.WirePlane * step;

                if (m_FloatingMovement)
                    m_FloatingMovement.Move(Time.deltaTime);
            }
        }

        /// <summary>
        /// Puts this data packet into seek mode
        /// </summary>
        /// <param name="target">Target to seek</param>
        /// <returns>If packet is in seek mode</returns>
        public bool SetSeekTarget(Transform target)
        {
            if (!m_SeekMovement)
                return false;

            CancelInvoke("Expire");

            m_Seek = true;
            m_SeekMovement.SetTarget(target);

            if (m_Renderer)
                m_Renderer.enabled = false;

            if (m_Trails)
                m_Trails.enabled = true;

            return true;
        }

        /// <summary>
        /// Set the animation speed of this packet
        /// </summary>
        /// <param name="speed">Speed of animation</param>
        public void SetAnimationSpeed(float speed)
        {
            if (m_Animator)
                m_Animator.speed = speed;
        }

        /// <summary>
        /// Notify from invoke that we have expired
        /// </summary>
        private void Expire()
        {
            if (OnExpired != null)
                OnExpired.Invoke(this);
        }

        /// <summary>
        /// Notify that we have reached our target after seeking
        /// </summary>
        private void TargetReached()
        {
            if (OnSeekComplete != null)
                OnSeekComplete.Invoke(this);
        }

        // IInteractive Interface
        public bool CanInteract(SparkJumper jumper)
        {
            return isActiveAndEnabled && !(jumper.isDrifting || m_Seek);
        }

        // IInteractive Interface
        public void OnInteract(SparkJumper jumper)
        {
            if (jumper.companion)
                SetSeekTarget(jumper.companion.transform);

            jumper.PlaySelectionSound(m_SelectedSound);

            if (OnCollected != null)
                OnCollected.Invoke(this);
        }
    }
}
