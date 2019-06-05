using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    public class WireManager : MonoBehaviour
    {
        public static Vector3 WirePlane = Vector3.forward;

        public Spark ActiveSpark { get { return m_ActiveWire ? m_ActiveWire.spark : null; } }

        [SerializeField] private float m_MinWireOffset = 5f;                        // The min amount of space between 2 wires
        [SerializeField] private float m_MaxWireOffset = 15f;                       // The max space between spawning a new wire from active wire
        [SerializeField] private float m_MinWireLength = 10f;                       // The min length of a wire
        [SerializeField] private float m_MaxWireLength = 30f;                       // The max length of a wire
        [SerializeField] private float m_MinWireSpawnRange = 1.5f;                  // The min time before spawning wires
        [SerializeField] private float m_MaxWireSpawnRange = 2.5f;                  // The max time before spawning wires
        [SerializeField] private int m_WiresToSpawn = 3;                            // The amount of wires to spawn
        
        [Header("Sparks")]
        [SerializeField] private Spark m_SparkPrefab;                       // Spark prefab to spawn for wires 

        private Wire m_ActiveWire;
        private Wire m_PendingWire;
        private Wire m_PreviousWire;
        [SerializeField] private float m_PercentBeforeSpawn = 0f;

        // Maps for wires surrounding the current active wire
        // 0 = Up - Right
        // 1 = Bottom - Right
        // 2 = Up - Left
        // 3 = Bottom - Left
        private Wire[] m_WireMap = new Wire[4];

        // Cached number of that are occupied
        private int m_QuadrantsOccupied = 0;

        [Header("Player")]
        [SerializeField] private SparkJumper m_JumperPrefab;

        private SparkJumper m_SparkJumper;

        void Awake()
        {
            for (int i = 0; i < m_WireMap.Length; ++i)
                m_WireMap[i] = null;
        }

        void Start()
        {
            m_ActiveWire = GenerateWire(Vector2.zero);
            m_PercentBeforeSpawn = Random.Range(m_MinWirePercent, m_MaxWirePercent);

            m_SparkJumper = Instantiate(m_JumperPrefab);
            m_ActiveWire.spark.AttachJumper(m_SparkJumper);

            m_SparkJumper.enabled = true;
        }

        void Update()
        {
            if (m_ActiveWire)
            {
                float alpha = m_ActiveWire.TickSpark(Time.deltaTime);
                if (alpha >= 1f)
                {
                    m_SparkJumper.transform.parent = null;

                    Destroy(m_ActiveWire.spark.gameObject);
                    Destroy(m_ActiveWire.gameObject);

                    m_ActiveWire = m_PendingWire;
                    m_PercentBeforeSpawn = Random.Range(m_MinWirePercent, m_MaxWirePercent);
                    m_PendingWire = null;

                    m_ActiveWire.spark.AttachJumper(m_SparkJumper);
                        
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

            GameObject gameObject = new GameObject("Wire");
            Wire wire = gameObject.AddComponent<Wire>();
            wire.transform.position = position;
            wire.m_Distance = Random.Range(m_MinWireLength, m_MaxWireLength);

            wire.SpawnSpark(m_SparkPrefab);

            return wire;
        }

        private int GenerateWires(int amount)
        {
            if (m_QuadrantsOccupied >= 4)
                return;

            int wiresGenerated = 0;

            // The quadrants that have no wires in them
            List<int> remainingWires = new List<int>(4 - m_QuadrantsOccupied);
            for (int i = 0; i < m_WireMap.Length; ++i)
            {
                if (m_WireMap[i] == null)
                    remainingWires.Add(i);
            }
                
            if (remainingWires.Count > 0)
            {

            }

            return 1;
        }

        /// <summary>
        /// Determines what quadrant a position is in based on an origin
        /// </summary>
        /// <param name="origin">Origin of the circle</param>
        /// <param name="position">Position relative to origin</param>
        /// <returns>Quadrant of the position</returns>
        private int GetCircleQuadrant(Vector2 origin, Vector2 position)
        {
            if (position != Vector2.zero)
            {
                Vector2 direction = (position - origin).normalized;

                int mask = 0;

                // Check if above or below
                if (Vector2.Dot(direction, Vector2.up) < 0)
                    mask |= 1;

                // Check if left or right
                if (Vector2.Dot(direction, Vector2.right) < 0)
                    mask |= 2;

                return mask;
            }

            return -1;
        }

        /// <summary>
        /// Generates a random direction that points towards given quadrant
        /// </summary>
        /// <param name="quadrant">Quadrant to point to</param>
        /// <returns>Direction to quandrant</returns>
        private Vector2 GetRandomDirectionInQuadrant(int quadrant)
        {
            float min = 0f;
            float max = Mathf.PI * 2f;

            switch (quadrant)
            {
                case 0:
                {
                    min = 0f;
                    max = Mathf.PI * 0.5f;
                    break;
                }
                case 1:
                {
                    min = Mathf.PI * 1.5f;
                    max = Mathf.PI * 2f;
                    break;
                }
                case 2:
                {
                    min = Mathf.PI * 0.5f;
                    max = Mathf.PI;
                    break;
                }
                case 3:
                {
                    min = Mathf.PI;
                    max = Mathf.PI * 1.5f;
                    break;
                }
            }

            float rand = Random.Range(min, max);
            return new Vector2(Mathf.Cos(rand), Mathf.Sin(rand));
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
