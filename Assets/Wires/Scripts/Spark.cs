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

        protected Wire m_Wire;                  // The wire we belong to
        protected SparkJumper m_Jumper;         // The jumper following this spark
        private SphereCollider m_Collider;      // Collider for tracing this spark

        void Awake()
        {
            m_Collider = GetComponent<SphereCollider>();
        }

        public void SetJumper(SparkJumper jumper)
        {
            m_Jumper = jumper;
        }

        public void AttachJumper(SparkJumper jumper)
        {
            jumper.transform.parent = transform;
            jumper.transform.localPosition = Vector3.zero;
        }

        void OnDrawGizmos()
        {
            if (m_Collider)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(transform.position, m_Collider.radius);
            }
        }
    }
}
