using System;
using UnityEngine;

namespace TO5.Wires.Legacy
{
    /// <summary>
    /// Sparks are attached to wires and will follow them until the end.
    /// </summary>
    [Obsolete, RequireComponent(typeof(SphereCollider))]
    public class SparkLegacy : MonoBehaviour
    {
        public float m_Speed = 2f;              // Speed of the spark

        public bool CanRide { get { return !m_PreviouslyOccupied; } }

        protected WireLegacy m_Wire;                  // The wire we belong to
        protected SparkJumperLegacy m_Jumper;         // The jumper following this spark
        private SphereCollider m_Collider;      // Collider for tracing this spark
        [SerializeField] private Material m_OccupiedMaterial;

        private bool m_PreviouslyOccupied = false;

        public SparkJumperLegacy Jumper { get { return m_Jumper; } }

        void Awake()
        {
            m_Collider = GetComponent<SphereCollider>();
        }

        public void InitializerSpark(WireLegacy wire)
        {
            m_Wire = wire;
            m_PreviouslyOccupied = false;
        }

        public WireLegacy GetWire()
        {
            return m_Wire;
        }

        public void SetJumper(SparkJumperLegacy jumper)
        {
            m_Jumper = jumper;
            m_PreviouslyOccupied = true;

            if (m_Jumper)
                m_Jumper.transform.position = transform.position;

            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer)
            {
                renderer.material.color = Color.red;

                if (m_OccupiedMaterial)
                    renderer.material = m_OccupiedMaterial;
            }
        }
    }
}
