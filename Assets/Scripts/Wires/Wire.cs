using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Liminal.Wires
{
    public class Wire : MonoBehaviour
    {
        private Vector3 m_Start;                // Start of this wire
        public float m_Distance = 15f;         // The distance of this wire

        private Spark m_Spark;

        void Awake()
        {
            m_Start = transform.position;
        }

        void Start()
        {
           
        }

        void Update()
        {

        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + (WireManager.WirePlane * m_Distance));
        }
    }
}
