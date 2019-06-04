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
        public float m_Speed = 2f;
        [NonSerialized] public Wire m_Wire;

        protected SparkJumper m_SparkJumper;
        protected SphereCollider m_Collider;

        void Awake()
        {
            m_Collider = GetComponent<SphereCollider>();
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
