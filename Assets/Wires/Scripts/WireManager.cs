using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Liminal.Wires
{
    public class WireManager : MonoBehaviour
    {
        public static Vector3 WirePlane = Vector3.forward;

        public Spark ActiveSpark { get { return m_ActiveWire ? m_ActiveWire.spark : null; } }

        [SerializeField] private float m_MinWireOffset = 5f;                        // The min amount of space between 2 wires
        [SerializeField] private float m_MaxWireOffset = 15f;                       // The max space between spawning a new wire from active wire
        [SerializeField] private float m_MinWireLength = 10f;                       // The min length of a wire
        [SerializeField] private float m_MaxWireLength = 30f;                       // The max length of a wire
        [SerializeField, Range(0f, 1f)] private float m_MinWirePercent = 0.2f;      // The min percentage of active wires length that must be travelled before spawning next wire
        [SerializeField, Range(0f, 1f)] private float m_MaxWirePercent = 0.8f;      // The max percentage of active wires length that must be travelled before spawning next wire
        
        [Header("Sparks")]
        [SerializeField] private Spark m_SparkPrefab;                       // Spark prefab to spawn for wires 

        private Wire m_ActiveWire;
        private Wire m_PendingWire;
        private Wire m_PreviousWire;
        [SerializeField] private float m_PercentBeforeSpawn = 0f;

        void Start()
        {
            m_ActiveWire = GenerateWire(Vector2.zero);
            m_PercentBeforeSpawn = Random.Range(m_MinWirePercent, m_MaxWirePercent);

        }

        void Update()
        {
            if (m_ActiveWire)
            {
                float alpha = m_ActiveWire.TickSpark(Time.deltaTime);
                if (alpha >= 1f)
                {
                    Destroy(m_ActiveWire.spark.gameObject);
                    Destroy(m_ActiveWire.gameObject);

                    m_ActiveWire = m_PendingWire;
                    m_PercentBeforeSpawn = Random.Range(m_MinWirePercent, m_MaxWirePercent);
                    m_PendingWire = null;

                    // We have to call tick here as not doing so will result in a frame without an update
                    m_ActiveWire.TickSpark(Time.deltaTime);
                }
                else
                {
                    if (alpha >= m_PercentBeforeSpawn && !m_PendingWire)
                    {
                        m_PendingWire = GenerateRandomWire();
                        m_PendingWire.m_Distance += m_ActiveWire.m_Distance * (1 - alpha);
                    }

                }
            }

            if (m_PendingWire)
                m_PendingWire.TickSpark(Time.deltaTime);
        }

        /// <summary>
        /// Generates a wire that is randomly offset from the active wire
        /// </summary>
        /// <returns>New wire if successfull</returns>
        private Wire GenerateRandomWire()
        {
            float rand = Random.Range(0f, Mathf.PI * 2f);

            Vector2 direction = new Vector2(Mathf.Cos(rand), Mathf.Sin(rand));
            float distance = Random.Range(m_MinWireOffset, m_MaxWireOffset);

            return GenerateWire(direction * distance);
        }

        /// <summary>
        /// Generates a wire that is offset from active wire
        /// </summary>
        /// <param name="offset">Offset from active wire</param>
        /// <returns>New wire if successfull</returns>
        private Wire GenerateWire(Vector2 offset)
        {
            Vector3 position = transform.position;
            if (ActiveSpark)
                position = ActiveSpark.transform.position;

            position += (Vector3)offset;

            GameObject gameObject = new GameObject();
            Wire wire = gameObject.AddComponent<Wire>();
            wire.transform.position = position;
            wire.m_Distance = Random.Range(m_MinWireLength, m_MaxWireLength);

            wire.SpawnSpark(m_SparkPrefab);

            return wire;
        }

        void OnDrawGizmos()
        {
            Vector3 center = transform.position;
            if (ActiveSpark)
                center = ActiveSpark.transform.position;

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
        }
    }
}
