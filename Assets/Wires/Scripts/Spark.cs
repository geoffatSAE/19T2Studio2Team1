using System;
using UnityEngine;

namespace TO5.Wires
{
    [RequireComponent(typeof(SphereCollider))]
    public class Spark : MonoBehaviour
    {
        public float m_Speed = 2f;
        [NonSerialized] public Wire m_Wire;

        public void AttachJumper(SparkJumper jumper)
        {
            jumper.transform.parent = transform;
            jumper.transform.localPosition = Vector3.zero;
        }

        void OnDrawGizmos()
        {
            SphereCollider collider = GetComponent<SphereCollider>();

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position, collider.radius);
        }
    }
}
