using System;
using UnityEngine;

namespace Liminal.Wires
{
    [RequireComponent(typeof(SphereCollider))]
    public class Spark : MonoBehaviour
    {
        public float m_Speed = 2f;
        [NonSerialized] public Wire m_Wire;

        void OnDrawGizmos()
        {
            SphereCollider collider = GetComponent<SphereCollider>();

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position, collider.radius);
        }
    }
}
