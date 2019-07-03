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
        
        /// <summary>
        /// Activates this data packet
        /// </summary>
        /// <param name="position">Position of packet</param>
        /// <param name="lifetime">Lifetime before expiration</param>
        public void Activate(Vector3 position, float lifetime)
        {
            gameObject.SetActive(true);

            transform.position = position;

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
            return isActiveAndEnabled;
        }
        
        // IInteractive Interface
        public void OnInteract(SparkJumper jumper)
        {
            if (OnCollected != null)
                OnCollected.Invoke(this);
        }
    }
}
