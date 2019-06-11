using System;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Sparks are attached to wires and will follow them until the end.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class Spark : MonoBehaviour
    {
        public float m_Speed = 2f;              // Speed of the spark

        public bool CanRide { get { return !m_PreviouslyOccupied; } }

        protected Wire m_Wire;                  // The wire we belong to
        protected SparkJumper m_Jumper;         // The jumper following this spark
        private SphereCollider m_Collider;      // Collider for tracing this spark

        private bool m_PreviouslyOccupied = false;

        public SparkJumper Jumper { get { return m_Jumper; } }

        void Awake()
        {
            m_Collider = GetComponent<SphereCollider>();
        }

        public void InitializerSpark(Wire wire)
        {
            m_Wire = wire;
            m_PreviouslyOccupied = false;
        }

        public Wire GetWire()
        {
            return m_Wire;
        }

        public void SetJumper(SparkJumper jumper)
        {
            m_Jumper = jumper;
            m_PreviouslyOccupied = true;

            if (m_Jumper)
                m_Jumper.transform.position = transform.position;

            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer)
                renderer.material.color = Color.red;
        }
    }
}
