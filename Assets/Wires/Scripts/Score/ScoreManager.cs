using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TO5.Wires
{
    /// <summary>
    /// Manages the score of the player and the active multiplier
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        /// <summary>
        /// Delegate to notify the players multiplier has changed
        /// </summary>
        /// <param name="multiplier">New multiplier</param>
        /// <param name="stage">New stage</param>
        public delegate void MultiplierStageUpdated(float multiplier, int stage);

        public MultiplierStageUpdated OnMultiplierUpdated;      // Event for when multiplier has changed

        [Header("Score")]
        [SerializeField] private float m_ScorePerSecond = 1f;           // Score player earns per second
        [SerializeField] private float m_JumpScore = 100f;              // Score player earns when jumping (not when forced to jump)
        [SerializeField] private float m_PacketScore = 250f;            // Score player earns when collecting a data packet

        [Header("Multiplier")]
        [SerializeField, Range(0, 32)] private int m_MultiplierStages = 2;      // The amount of stages for the multiplier
        [SerializeField] private float m_MultiplierIncreaseInterval = 15f;      // Seconds before players multiplier increased

        [Header("Packets")]
        [SerializeField] private DataPacket m_PacketPrefab;                         // Prefab for data packets
        [SerializeField] private float m_PacketSpace = 2f;                          // The space packet should have (avoid overlap)
        [SerializeField] private PacketStageProperties[] m_PacketProperties;        // Properties for packet behavior for each multiplier stage

        [Header("Boost")]
        [SerializeField] private float m_BoostChargeRate = 0.83f;                   // Boost player earns per second
        [SerializeField] private float m_BoostDepletionRate = 10f;                  // Boost player consumes per second (when active)
        [SerializeField] private float m_BoostMultiplier = 2f;                      // Multipler all score is scaled by when boost is active
        [SerializeField] private float m_BoostPerPacket = 5f;                       // Boost player earns when collecting a packet

        // Space required for packets
        public float packetSpace { get { return m_PacketSpace; } }

        // Amount of packets active
        public int activePackets { get { return m_DataPackets.activeCount; } }

        private ObjectPool<DataPacket> m_DataPackets = new ObjectPool<DataPacket>();    // Packets being managed
        private PacketStageProperties m_ActivePacketProperties;                         // Properties for current stage
        private int m_PacketSpawnsSinceLastCluster = 0;                                 // Amount of random packets spawn attempts since the last packet cluster

        [Header("UI")]
        public Text m_ScoreText;                    // Text for writing score
        public Text m_MultiplierText;               // Text for writing multiplier

        //#if UNITY_EDITOR
        [SerializeField] private Text m_DebugText;      // Text for writing debug data
       // #endif

        // Current multiplier
        public float multiplier { get { return m_Multiplier; } }

        // Multipliers current stage
        public int multiplierStage { get { return m_Stage; } }

        // Total multipler (default * boost)
        public float totalMultiplier { get { return m_Multiplier * (m_BoostActive ? m_BoostMultiplier : 1f); } }

        // If boost is ready
        public bool boostReady { get { return m_Boost >= 100f; } }

        // If boost is active
        public bool boostActive { get { return m_BoostActive; } }

        private WireManager m_WireManager;              // Manager for wires
        private SparkJumper m_SparkJumper;              // Players spark jumper
        private float m_Score;                          // Players score
        private float m_Multiplier;                     // Players multiplier
        private int m_Stage;                            // Multiplier stage
        private float m_Boost;                          // Players boost (Between 0 and 100)
        private bool m_BoostActive;                     // If boost is active
        private Coroutine m_MultiplierTick;             // Coroutine for multipliers tick
        private Coroutine m_PacketSpawn;                // Coroutine for spawning packets

        void Awake()
        {
            enabled = false;
        }

        void Update()
        {
            m_Score += m_ScorePerSecond * totalMultiplier * Time.deltaTime;

            // Tick boost
            if (m_SparkJumper)
            {
                if (m_BoostActive)
                {
                    m_Boost = Mathf.Max(0, m_Boost - (m_BoostDepletionRate * Time.deltaTime));
                    m_BoostActive = m_Boost > 0f;
                }
                else if (!m_SparkJumper.isDrifting)
                {
                    m_Boost = Mathf.Min(100, m_Boost + (m_BoostChargeRate * Time.deltaTime));
                }
            }

            // Tick packets
            {
                float step = Time.deltaTime;

                for (int i = 0; i < m_DataPackets.activeCount; ++i)
                {
                    DataPacket packet = m_DataPackets.GetObject(i);
                    packet.TickPacket(step);
                }
            }

            if (m_ScoreText)
                m_ScoreText.text = string.Format("Score: {0}\nMultiplier: {1}", Mathf.FloorToInt(m_Score), totalMultiplier);

           // #if UNITY_EDITOR
                // Debug text
                if (m_DebugText)
                m_DebugText.text = string.Format("Score: {0}\nMultiplier: {1}\nMultiplier Stage: {2}\nPackets Pool Size: {3}\nPackets Active: {4}\nBoost Meter: {5}\nBoost Active: {6}", 
                    Mathf.FloorToInt(m_Score), m_Multiplier, m_Stage, m_DataPackets.Count, m_DataPackets.activeCount, Mathf.FloorToInt(m_Boost), m_BoostActive);
           // #endif
        }

        /// <summary>
        /// Initialize manager to work with wire manager
        /// </summary>
        /// <param name="wireManager">Wire manager</param>
        public void Initialize(WireManager wireManager)
        {
            m_WireManager = wireManager;
            m_SparkJumper = wireManager.sparkJumper;

            if (m_SparkJumper)
                m_SparkJumper.OnActivateBoost = ActivateBoost;

            m_Score = 0f;
            m_Multiplier = 1f;
            m_Stage = 0;

            m_Boost = 0f;
            m_BoostActive = false;

            m_PacketSpawnsSinceLastCluster = 0;
        }

        /// <summary>
        /// Enables scoring functionality
        /// </summary>
        /// <param name="reset">If properties should reset</param>
        public void EnableScoring(bool reset)
        {
            enabled = true;

            if (reset)
            {
                m_Score = 0f;
                m_Multiplier = 1f;
                m_Score = 0;

                m_Boost = 0f;
                m_BoostActive = false;
            }

            m_MultiplierTick = StartCoroutine(MultiplierTickRoutine());
            m_PacketSpawn = StartCoroutine(PacketSpawnRoutine());
        }

        /// <summary>
        /// Disables scoring functionality
        /// </summary>
        public void DisableScoring()
        {
            StopCoroutine(m_MultiplierTick);
            StopCoroutine(m_PacketSpawn);
            m_MultiplierTick = null;
            m_PacketSpawn = null;

            enabled = false;
        }

        /// <summary>
        /// Increases the multiplier by amount of stages
        /// </summary>
        /// <param name="stages">Stages to increase by</param>
        public void IncreaseMultiplier(int stages = 1)
        {
            SetMultiplierStage(m_Stage + stages);
        }

        /// <summary>
        /// Decreases the multiplier by amount of stages
        /// </summary>
        /// <param name="stages"></param>
        public void DecreaseMultiplier(int stages = 1)
        {
            SetMultiplierStage(m_Stage - stages);

            // Reset the multiplier tick routine
            StopCoroutine(m_MultiplierTick);
            m_MultiplierTick = StartCoroutine(MultiplierTickRoutine());
        }

        /// <summary>
        /// Sets the multiplier stage, updates multiplier value
        /// </summary>
        /// <param name="stage">Stage to set</param>
        private void SetMultiplierStage(int stage)
        {
            if (stage != m_Stage)
            {
                m_Stage = Mathf.Clamp(stage, 0, m_MultiplierStages);
                m_Multiplier = (1 << m_Stage);

                m_ActivePacketProperties = GetPacketProperties(m_Stage);

                if (OnMultiplierUpdated != null)
                    OnMultiplierUpdated.Invoke(m_Multiplier, m_Stage);
            }
        }

        /// <summary>
        /// Resets the multiplier stage
        /// </summary>
        public void ResetMultiplier()
        {
            m_Multiplier = 1f;
            m_Stage = 0;
        }

        /// <summary>
        /// Adds jump points to players score
        /// </summary>
        public void AwardJumpPoints()
        {
            m_Score += (m_JumpScore * totalMultiplier);
        }

        /// <summary>
        /// Tick routine for increasing multiplier
        /// </summary>
        /// <returns></returns>
        private IEnumerator MultiplierTickRoutine()
        {
            while (enabled)
            {
                yield return new WaitForSeconds(m_MultiplierIncreaseInterval);
                IncreaseMultiplier(1);

                // No point in looping if at max stage
                if (m_Stage == m_MultiplierStages)
                    break;
            }
        }

        /// <summary>
        /// Generates a data packet randomly located in the world
        /// </summary>
        /// <param name="tryCluster">If a cluster of packets can possibly be spawned</param>
        /// <returns>Packet or null</returns>
        private DataPacket GenerateRandomPacket(bool tryCluster)
        {
            PacketStageProperties packetProps = GetStagePacketProperties();

            // Try to spawn a cluster of packets if possible
            if (tryCluster && m_PacketSpawnsSinceLastCluster >= packetProps.m_ClusterRate)
            {
                bool cluster = packetProps.m_ClusterChance > 0f ? Random.Range(0f, 100f) < (packetProps.m_ClusterChance * 100f) : false;
                if (cluster)
                {
                    DataPacket packet = GeneratePacketCluster();
                    m_PacketSpawnsSinceLastCluster = 0;

                    return packet;
                }   
            }

            const int maxAttempts = 5;
            Vector3 spawnCenter = m_WireManager.GetSpawnCircleCenter() + WireManager.WirePlane * (m_WireManager.segmentLength * packetProps.m_MinSpawnOffset);

            Vector3 position = Vector3.zero;
            bool success = false;

            // We don't want to loop to many times
            int attempts = 0;
            while (++attempts <= maxAttempts)
            {
                Vector2 circleOffset = m_WireManager.GetRandomSpawnCircleOffset(m_PacketSpace);
                position = spawnCenter + new Vector3(circleOffset.x, circleOffset.y, 0f);

                // We expect to spawn far in front of wires
                success = m_WireManager.HasSpaceAtLocation(position, true);
                if (success)
                    break;
            }

            if (!success)
            {
                Debug.LogWarning(string.Format("Failed to generate packet after {0} attempts", maxAttempts), this);
                return null;
            }

            float speed = Random.Range(packetProps.m_MinSpeed, packetProps.m_MaxSpeed);

            ++m_PacketSpawnsSinceLastCluster;

            return GeneratePacket(position, speed, packetProps.m_Lifetime);
        }

        /// <summary>
        /// Generates a data packet, immediately activating it
        /// </summary>
        /// <param name="position">Position of the packet</param>
        /// <param name="speed">Speed of the packet</param>
        /// <param name="lifetime">Lifetime of the packet</param>
        /// <returns>Packet or null</returns>
        private DataPacket GeneratePacket(Vector3 position, float speed, float lifetime)
        {
            DataPacket packet = GetPacket();
            if (!packet)
                return null;

            packet.Activate(position, speed, lifetime);

            return packet;
        }

        /// <summary>
        /// Generates a random cluster of packets
        /// </summary>
        /// <returns>Last packet generated or null</returns>
        private DataPacket GeneratePacketCluster()
        {
            PacketStageProperties packetProps = GetStagePacketProperties();

            const int maxAttempts = 5;
            Vector3 spawnCenter = m_WireManager.GetSpawnCircleCenter() + WireManager.WirePlane * (m_WireManager.segmentLength * packetProps.m_MinSpawnOffset);     

            DataPacket packet = null;

            #if UNITY_EDITOR
            int packetsSpawned = 0;
            #endif

            int clusterSize = Random.Range(packetProps.m_MinPacketsPerCluster, packetProps.m_MaxPacketsPerCluster + 1);
            for (int i = 0; i < clusterSize; ++i)
            {
                Vector3 position = Vector3.zero;
                bool success = false;

                // We don't want to loop to many times
                int attempts = 0;
                while (++attempts <= maxAttempts)
                {
                    // Offset along plane
                    int randomSegmentOffset = Random.Range(-packetProps.m_ClusterSpawnRange, packetProps.m_ClusterSpawnRange + 1);
                    Vector3 planeOffset = WireManager.WirePlane * (m_WireManager.segmentLength * randomSegmentOffset);

                    // Offset inside circle
                    Vector2 circleOffset = m_WireManager.GetRandomSpawnCircleOffset(m_PacketSpace);

                    position = spawnCenter + planeOffset + new Vector3(circleOffset.x, circleOffset.y, 0f);

                    // We expect to spawn far in front of wires
                    success = m_WireManager.HasSpaceAtLocation(position, true);
                    if (success)
                        break;
                }

                if (!success)
                {
                    Debug.LogWarning(string.Format("Failed to generate packet after {0} attempts for cluster", maxAttempts), this);
                    continue;
                }

                float speed = Random.Range(packetProps.m_MinSpeed, packetProps.m_MaxSpeed);

                packet = GeneratePacket(position, speed, packetProps.m_Lifetime);

                #if UNITY_EDITOR
                if (packet != null)
                    ++packetsSpawned;
                #endif
            }

            #if UNITY_EDITOR
            Debug.Log(string.Format("Packet Cluster Spawn Results - Cluster Size: {0}, Packets Spawned: {1}", clusterSize, packetsSpawned));
            #endif

            return packet;
        }

        /// <summary>
        /// Helper for getting a packet from the pool (spawns one if needed)
        /// </summary>
        /// <returns>Packet or null</returns>
        private DataPacket GetPacket()
        {
            if (m_DataPackets.canActivateObject)
                return m_DataPackets.ActivateObject();

            if (!m_PacketPrefab)
            {
                Debug.LogError("Unable to spawn data prefab as prefab is invalid", this);
                return null;
            }

            Vector3 position = m_WireManager ? m_WireManager.disabledSpot : Vector3.zero;
            DataPacket packet = Instantiate(m_PacketPrefab, position, Quaternion.identity);

            // Hook events
            {
                packet.OnCollected += PacketCollected;
                packet.OnExpired += PacketExpired;
            }

            m_DataPackets.Add(packet);
            m_DataPackets.ActivateObject();

            return packet;
        }

        /// <summary>
        /// Gets packet generation properties for index. Checks if properties exist for
        /// index, either creating a new set or getting best replacement if it doesn't
        /// </summary>
        /// <param name="index">Index of properties</param>
        /// <returns>Valid properties</returns>
        private PacketStageProperties GetPacketProperties(int index)
        {
            if (m_PacketProperties == null || m_PacketProperties.Length == 0)
                return new PacketStageProperties();

            // We use the latest properties if index is still out of range
            index = Mathf.Clamp(index, 0, m_PacketProperties.Length - 1);
            return m_PacketProperties[index];
        }

        /// <summary>
        /// Get packet generation parameters for current multiplier stage
        /// </summary>
        /// <returns>Valid properties</returns>
        private PacketStageProperties GetStagePacketProperties()
        {
            if (m_ActivePacketProperties != null)
                return m_ActivePacketProperties;

            return GetPacketProperties(m_Stage);
        }

        /// <summary>
        /// Notify that player has collected given packet
        /// </summary>
        private void PacketCollected(DataPacket packet)
        {
            m_Score += m_PacketScore * totalMultiplier;

            // Only give boost when not active
            if (!m_BoostActive)
                m_Boost = Mathf.Clamp(m_Boost + m_BoostPerPacket, 0, 100f);

            PacketExpired(packet);
        }

        /// <summary>
        /// Notify that given packet has expired
        /// </summary>
        private void PacketExpired(DataPacket packet)
        {
            packet.Deactivate();
            m_DataPackets.DeactivateObject(packet);
        }

        /// <summary>
        /// Notify from player that they wish to activate boost
        /// </summary>
        /// <returns>If boost was activated</returns>
        private bool ActivateBoost()
        {
            if (!m_BoostActive && boostReady)
            {
                m_BoostActive = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Routine for spawning packets
        /// </summary>
        private IEnumerator PacketSpawnRoutine()
        {
            while (enabled)
            {
                PacketStageProperties packetProps = GetStagePacketProperties();
                float delay = Random.Range(packetProps.m_MinSpawnInterval, packetProps.m_MaxSpawnInterval);

                yield return new WaitForSeconds(delay);

                GenerateRandomPacket(true);
            }
        }

        /// <summary>
        /// Gets active packet at index
        /// </summary>
        /// <param name="index">Index of packet</param>
        /// <returns>Active packet or null</returns>
        // TODO: This function only exists for HasSpaceAtLocation in WireManager
        public DataPacket GetActivePacket(int index)
        {
            if (index < m_DataPackets.activeCount)
                return m_DataPackets.GetObject(index);

            return null;
        }
    }
}
