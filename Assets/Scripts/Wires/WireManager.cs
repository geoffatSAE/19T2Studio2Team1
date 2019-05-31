using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Liminal.Wires
{
    public class WireManager : MonoBehaviour
    {
        public static Vector3 WirePlane = Vector3.forward;

        private List<Wire> m_Wires;
        private Wire m_ActiveWire;

        [SerializeField] private float m_WireRadius = 1.5f;           
        [SerializeField] private float m_MinWireOffset = 5f;
        [SerializeField] private float m_MaxWireOffset = 15f;

        void Awake()
        {
            GenerateWires(6);
        }

        private void GenerateWires(int amount)
        {
            Vector2 center = transform.position;

            m_Wires = new List<Wire>(amount);
            for (int i = 0; i < amount; ++i)
            {
                int attempts = 0;
                const int maxAttempts = 10;

                while (attempts < maxAttempts)
                { 
                    Vector2 position = center + (Random.insideUnitCircle.normalized * Random.Range(m_MinWireOffset, m_MaxWireOffset));
                    if (CanFitWireAt(position))
                    {
                        GameObject gameObject = new GameObject("Wire" + i);
                        Wire wire = gameObject.AddComponent<Wire>();

                        Vector3 wirePosition = position;
                        wirePosition.z = transform.position.z;

                        wire.m_Distance = 1000f;
                        gameObject.transform.position = wirePosition;

                        m_Wires.Add(wire);

                        break;
                    }

                    ++attempts;
                }
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                foreach (Wire wire in m_Wires)
                    Destroy(wire.gameObject);

                GenerateWires(6);
            }
        }

        private bool CanFitWireAt(Vector2 position)
        {
            float diameter = m_WireRadius + m_WireRadius;
            foreach (Wire wire in m_Wires)
            {
                Vector2 wirePosition = wire.transform.position;
                float distance = (wirePosition - position).sqrMagnitude;

                if (distance < (diameter * diameter))
                    return false;
            }

            return true;
        }

        void OnDrawGizmos()
        {
            Vector3 center = transform.position;
            if (m_ActiveWire)
            {
                center = m_ActiveWire.transform.position;
            }

            Gizmos.color = Color.green;

            const int segments = 16;
            const float step = Mathf.PI * 2f / segments;
            for (int i = 0; i < segments; ++i)
            {
                float crad = step * i;
                float nrad = step * ((i + 1) % segments);

                Vector3 cdir = new Vector3(Mathf.Cos(crad), Mathf.Sin(crad), 0f);
                Vector3 ndir = new Vector3(Mathf.Cos(nrad), Mathf.Sin(nrad), 0f);

                // Inner border
                {
                    Vector3 start = center + cdir * m_MinWireOffset;
                    Vector3 end = center + ndir * m_MinWireOffset;
                    Gizmos.DrawLine(start, end);
                }

                // Outer border
                {
                    Vector3 start = center + cdir * m_MaxWireOffset;
                    Vector3 end = center + ndir * m_MaxWireOffset;
                    Gizmos.DrawLine(start, end);
                }
            }

            Gizmos.color = Color.red;
            foreach (Wire wire in m_Wires)
            {
                center = wire.transform.position;

                for (int i = 0; i < segments; ++i)
                {
                    float crad = step * i;
                    float nrad = step * ((i + 1) % segments);

                    Vector3 cdir = new Vector3(Mathf.Cos(crad), Mathf.Sin(crad), 0f);
                    Vector3 ndir = new Vector3(Mathf.Cos(nrad), Mathf.Sin(nrad), 0f);

                    Vector3 start = center + cdir * m_WireRadius;
                    Vector3 end = center + ndir * m_WireRadius;
                    Gizmos.DrawLine(start, end);
                }
            }
        }
    }
}
