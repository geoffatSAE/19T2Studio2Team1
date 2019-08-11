using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Handles fireworks used in finale of Wires. This is a workaround
    /// for the ParticleSystem component lacking a OnParticleDeath event
    /// </summary>
    public class FireworksHandler : MonoBehaviour
    {
        [SerializeField] private FireworksObject m_Prefab;                                  // Prefab of fireworks to spawn
        [SerializeField] private Vector3 m_InnerBorder = new Vector3(2f, 2f, 5f);           // Inner area that no fireworks will spawn in
        [SerializeField] private Vector3 m_OuterBorder = new Vector3(6f, 2f, 5f);           // Outer area that no fireworks will spawn outside of
        [SerializeField] private float m_MinInterval = 0.25f;                               // Min interval between spawning fireworks
        [SerializeField] private float m_MaxInterval = 0.75f;                               // Max interval between spawning fireworks

        ObjectPool<FireworksObject> m_Fireworks = new ObjectPool<FireworksObject>();        // Fireworks that exist in the world

        void Awake()
        {
            // We spawn one firework now so we can preload assets to avoid hitches
            GetFireworksObject();

            enabled = false;
        }

        /// <summary>
        /// Activates the generation of fireworks
        /// </summary>
        public void Activate()
        {
            if (!IsInvoking())
            {
                enabled = true;
                GenerateFireworks();
            }
        }

        /// <summary>
        /// Generates and activates a single fireworks object
        /// </summary>
        private void GenerateFireworks()
        {
            FireworksObject fireworks = GetFireworksObject();
            if (fireworks)
            {
                Vector3 localPosition = new Vector3(
                    Random.Range(m_InnerBorder.x, m_OuterBorder.x),
                    Random.Range(m_InnerBorder.y, m_OuterBorder.y),
                    Random.Range(m_InnerBorder.z, m_OuterBorder.z));

                // We expect sign to never return zero (0 should return 1)
                localPosition.Scale(new Vector3(
                    Mathf.Sign(Random.Range(-1f, 1f)),
                    Mathf.Sign(Random.Range(-1f, 1f)),
                    Mathf.Sign(Random.Range(-1f, 1f))));

                fireworks.Play(localPosition);
            }

            // Keep invoking till disabled
            Invoke("GenerateFireworks", Random.Range(m_MinInterval, m_MaxInterval));
        }

        /// <summary>
        /// Gets and activates a fireworks object to use. Creating one if needed
        /// </summary>
        /// <returns>Fireworks object or null</returns>
        private FireworksObject GetFireworksObject()
        {
            if (m_Fireworks.canActivateObject)
                return m_Fireworks.ActivateObject();

            if (!m_Prefab)
                return null;

            FireworksObject fireworks = Instantiate(m_Prefab, transform);
            fireworks.OnFireworksCompleted += OnFireworksCompleted;

            m_Fireworks.Add(fireworks);
            m_Fireworks.ActivateObject();

            return fireworks;
        }

        /// <summary>
        /// Notify that a fireworks object has completed simualtion
        /// </summary>
        /// <param name="fireworks">Fireworks object that has finished</param>
        private void OnFireworksCompleted(FireworksObject fireworks)
        {
            m_Fireworks.DeactivateObject(fireworks);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.78f, 0.20f, 0.41f);
            Gizmos.DrawWireCube(transform.position, m_InnerBorder * 2f);
            Gizmos.DrawWireCube(transform.position, m_OuterBorder * 2f);
        }
    }
}
