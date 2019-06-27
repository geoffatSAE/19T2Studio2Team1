using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires.Legacy
{
    [System.Obsolete]
    public class WireManagerLegacy : MonoBehaviour
    {
        public static Vector3 WirePlane = Vector3.forward;

        public SparkLegacy ActiveSpark { get { return m_ActiveWire ? m_ActiveWire.spark : null; } }

        [SerializeField] private float m_MinWireOffset = 5f;                        // The min amount of space between 2 wires
        [SerializeField] private float m_MaxWireOffset = 15f;                       // The max space between spawning a new wire from active wire
        [SerializeField] private float m_MinWireLength = 10f;                       // The min length of a wire
        [SerializeField] private float m_MaxWireLength = 30f;                       // The max length of a wire
        [SerializeField] private float m_MinWireSpawnRange = 1.5f;                  // The min time before spawning wires
        [SerializeField] private float m_MaxWireSpawnRange = 2.5f;                  // The max time before spawning wires
        [SerializeField] private float m_WireSpawnDelay = 1f;                       // Small delay before then generating more wires
        [SerializeField] private float m_SparkSpawnDelay = 1.5f;                      // The max delay before a wire spawns its spark
        [SerializeField] private int m_WiresToSpawn = 3;                            // The amount of wires to spawn
        
        [Header("Sparks")]
        [SerializeField] private SparkLegacy m_SparkPrefab;                       // Spark prefab to spawn for wires 

        [Header("Wires")]
        [SerializeField] private WireLegacy m_WirePrefab;
        private List<WireLegacy> m_Wires = new List<WireLegacy>();
        private WireLegacy m_ActiveWire;     

        [Header("Player")]
        [SerializeField] private SparkJumperLegacy m_JumperPrefab;
        private SparkJumperLegacy m_SparkJumper;

        void Awake()
        {
            m_SparkJumper = Instantiate(m_JumperPrefab);
            m_SparkJumper.OnJumpToSpark += NotifyActiveSparkChanged;
        }

        void Start()
        {
            m_ActiveWire = GenerateWire(transform.position, Random.Range(m_MinWireLength, m_MaxWireLength), true);
            if (!m_ActiveWire.spark)
                m_ActiveWire.SpawnSpark(m_SparkPrefab);

            m_Wires.Add(m_ActiveWire);

            m_SparkJumper.JumpToSpark(m_ActiveWire.spark);
            m_SparkJumper.enabled = true;
            
            StartCoroutine(LatentGenerateWires());
        }

        void Update()
        {
            List<WireLegacy> finishedWires = new List<WireLegacy>();

            foreach (WireLegacy wire in m_Wires)
            {
                float progress = wire.TickSpark(Time.deltaTime);
                if (progress >= 1f)
                {
                    if (wire == m_ActiveWire)
                    {
                        WireLegacy closestWire = FindClosestWireTo(wire);
                        if (closestWire)
                            m_SparkJumper.JumpToSpark(closestWire.spark);
                    }

                    finishedWires.Add(wire);
                }
            }

            foreach (WireLegacy wire in finishedWires)
            {
                m_Wires.Remove(wire);
                Destroy(wire.gameObject);
            }
        }

        /// <summary>
        /// Generates a wire that is randomly offset from the active wire
        /// </summary>
        /// <returns>New wire if successfull</returns>
        private WireLegacy GenerateRandomWire()
        {
            float rand = Random.Range(0f, Mathf.PI * 2f);

            Vector2 direction = new Vector2(Mathf.Cos(rand), Mathf.Sin(rand));
            float distance = Random.Range(m_MinWireOffset, m_MaxWireOffset);

            return GenerateWire(direction * distance);
        }

        /// <summary>
        /// Latent action to spawn new wires, this is called when the match starts
        /// </summary>
        /// <returns></returns>
        IEnumerator LatentGenerateWires()
        {
            while (true)
            {
                float rand = Random.Range(m_MinWireSpawnRange, m_MaxWireSpawnRange);
                yield return new WaitForSeconds(rand);

                GenerateWires(m_WiresToSpawn);

                yield return new WaitForSeconds(m_WireSpawnDelay);
            }
        }

        /// <summary>
        /// Generates a wire that is offset from active wire
        /// </summary>
        /// <param name="offset">Offset from active wire</param>
        /// <returns>New wire if successfull</returns>
        private WireLegacy GenerateWire(Vector2 offset)
        {
            Vector3 position = transform.position;
            if (ActiveSpark)
                position = ActiveSpark.transform.position;

            position += (Vector3)offset;

            GameObject gameObject = new GameObject("Wire");
            WireLegacy wire = gameObject.AddComponent<WireLegacy>();
            wire.transform.position = position;
            wire.m_Distance = Random.Range(m_MinWireLength, m_MaxWireLength);

            wire.SpawnSpark(m_SparkPrefab);

            return wire;
        }

        private int GenerateWires(int amount)
        {
            // Max attempts for generating wires
            const int maxAttempts = 5;

            int wiresGenerated = 0;
            Vector3 origin = ActiveSpark ? ActiveSpark.transform.position : transform.position;

            for (int i = 0; i < amount; ++i)
            {
                bool validPos = false;
                Vector3 wirePos = origin;

                // Try to find a valid position to spawn this wire
                for (int j = 0; j < maxAttempts; ++j)
                {
                    Vector2 direction = GetRandomDirectionInCircle();
                    float distance = Random.Range(m_MinWireOffset, m_MaxWireOffset);

                    wirePos += (Vector3)(direction * distance);
                    if (HasSpaceAtLocation(wirePos))
                    {
                        validPos = true;
                        break;
                    }
                }

                if (!validPos)
                {
                    Debug.Log(string.Format("Tried {0} times to generate wire but failed", maxAttempts));
                    continue;
                }

                float wireDis = Random.Range(m_MinWireLength, m_MaxWireLength);

                WireLegacy wire = GenerateWire(wirePos, wireDis);
                if (wire)
                {
                    m_Wires.Add(wire);
                    ++wiresGenerated;
                }
            }

            return wiresGenerated;
        }

        /// <summary>
        /// Generates a new wire by spawning one or retrieving one from the pool
        /// </summary>
        /// <param name="position">Starting position of the wire</param>
        /// <param name="distance">Distane of the wire</param>
        /// <param name="ignoreDelay">If spark delay should be ignored</param>
        /// <returns>New wire</returns>
        private WireLegacy GenerateWire(Vector3 position, float distance, bool ignoreDelay = false)
        {
            // TODO: Pooling
            WireLegacy wire = Instantiate(m_WirePrefab);
            Transform transform = wire.transform;

            float sparkDelay = 0f;
            if (!ignoreDelay)
                sparkDelay = Random.Range(0f, m_SparkSpawnDelay);

            wire.SetPositionAndDistance(position, distance);
            wire.InitializeWire(distance, m_SparkPrefab, sparkDelay);

            return wire;
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

        /// <summary>
        /// Generates a random direction of circle on the wire plane
        /// </summary>
        /// <returns>Random direction</returns>
        private Vector3 GetRandomDirectionInCircle()
        {
            float rand = Random.Range(0f, Mathf.PI * 2f);
            return new Vector2(Mathf.Cos(rand), Mathf.Sin(rand));
        }

        private bool HasSpaceAtLocation(Vector3 position)
        {
            Vector2 posXY = position;
            float minDistancedSqr = m_MinWireOffset * m_MinWireOffset;

            foreach (WireLegacy wire in m_Wires)
            {
                bool validXY = true;

                Transform transform = wire.transform;
                Vector2 xy = transform.position;

                // If position is too close on Wire Plane
                float distance = (xy - posXY).sqrMagnitude;
                if (distance < minDistancedSqr)
                    validXY = false;

                Vector3 end = wire.GetEnd();

                // Position is within this wires range, end if we are already to close
                if (position.z < end.z && position.z > transform.position.z)
                    if (!validXY)
                        return false;
                    else
                    if (position.z > end.z)
                        validXY = Mathf.Abs(position.z - end.z) > m_MinWireOffset;
                    else
                        validXY = Mathf.Abs(position.z - transform.position.z) > m_MinWireOffset;

                if (!validXY)
                    return false;
            }

            // No wires are within range of point
            return true;
        }

        /// <summary>
        /// Finds the closest wire to given wire based on XY axes
        /// </summary>
        /// <param name="wire">Wire to search from</param>
        /// <returns></returns>
        private WireLegacy FindClosestWireTo(WireLegacy wire)
        {
            if (!wire)
                return null;

            WireLegacy closest = null;
            float distance = float.MaxValue;
            Vector3 origin = wire.spark ? wire.spark.transform.position : wire.transform.position;
            
            foreach (WireLegacy cand in m_Wires)
            {
                if (cand == wire)
                    continue;

                // Wire might not have a spark yet
                if (cand.spark)
                {
                    float dis = (cand.spark.transform.position - m_SparkJumper.transform.position).sqrMagnitude;
                    if (dis < distance)
                    {
                        closest = cand;
                        distance = dis;
                    }
                }
            }

            return closest;
        }

        private void NotifyActiveSparkChanged(SparkLegacy spark)
        {
            if (spark)
                m_ActiveWire = spark.GetWire();
            else
                m_ActiveWire = null;
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
