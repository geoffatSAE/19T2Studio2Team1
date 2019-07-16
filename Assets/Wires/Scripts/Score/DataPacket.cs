using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    [RequireComponent(typeof(SphereCollider))]
    public class DataPacket : MonoBehaviour, IInteractive
    {
        /// <summary>
        /// Delegate for when data packet has either expired or been collected by the player
        /// </summary>
        /// <param name="packet">Packet that has been collected</param>
        public delegate void PacketCollected(DataPacket packet);

        public PacketCollected OnCollected;     // Event for when player collects this packet
        public PacketCollected OnExpired;       // Event for when this packet expires

        [SerializeField] private AudioSource m_Ambience;

        private float m_Speed;          // The speed of this packet
        
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
            m_Speed = speed;

            if (m_Ambience)
                m_Ambience.Play();

            StartCoroutine(ExpireRoutine(lifetime));
        }

        /// <summary>
        /// Deactivates this data packet
        /// </summary>
        public void Deactivate()
        {
            StopCoroutine("ExpireRoutine");

            if (m_Ambience)
                m_Ambience.Stop();

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Ticks this packet, moving the packet backwards
        /// </summary>
        /// <param name="step">Amount to move packet by (scaled by speed)</param>
        public void TickPacket(float step)
        {
            // We move in the opposite direction of sparks (so we subtract)
            step *= m_Speed;
            transform.position -= WireManager.WirePlane * step;    
        }

        /// <summary>
        /// Routine for waiting for lifetime to expire
        /// </summary>
        /// <param name="lifetime">Packets lifetime</param>
        private IEnumerator ExpireRoutine(float lifetime)
        {
            yield return new WaitForSeconds(lifetime);

            if (OnExpired != null)
                OnExpired.Invoke(this);
        }

        // IInteractive Interface
        public bool CanInteract(SparkJumper jumper)
        {
            return isActiveAndEnabled && !jumper.isDrifting;
        }
        
        // IInteractive Interface
        public void OnInteract(SparkJumper jumper)
        {
            if (OnCollected != null)
                OnCollected.Invoke(this);
        }
    }
}
