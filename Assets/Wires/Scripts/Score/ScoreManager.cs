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
        [SerializeField] private DataPacket m_PacketPrefab;             // Prefab for data packets
        [SerializeField] private int m_MinPacketSpawnInterval = 10;     // Min interval between spawning packets
        [SerializeField] private int m_MaxPacketSpawnInterval = 20;     // Max interval between spawning packets
        [SerializeField] private int m_MinPacketSpawnOffset = 20;       // Min segments in front of player to spawn
        [SerializeField] private float m_PacketSpace = 2f;              // The space packet should have (avoid overlap)
        [SerializeField] private float m_MinPacketSpeed = 1f;           // Min speed of a packet
        [SerializeField] private float m_MaxPacketSpeed = 2.5f;         // Max speed of a packet
        [SerializeField] private float m_PacketLifetime = 30f;          // How long data packets last for before expiring

        // Space required for packets
        public float packetSpace { get { return m_PacketSpace; } }

        // Amount of packets active
        public int activePackets { get { return m_DataPackets.activeCount; } }

        private ObjectPool<DataPacket> m_DataPackets = new ObjectPool<DataPacket>();    // Packets being managed

        [Header("UI")]
        public Text m_ScoreText;                    // Text for writing score
        public Text m_MultiplierText;               // Text for writing multiplier

        //#if UNITY_EDITOR
        [SerializeField] private Text m_DebugText;      // Text for writing debug data
       // #endif

        // Scores current multiplier
        public float multiplier { get { return m_Multiplier; } }

        // Multipliers current stage
        public int multiplierStage { get { return m_Stage; } }

        private WireManager m_WireManager;              // Manager for wires
        private float m_Score;                          // Players score
        private float m_Multiplier;                     // Players multiplier
        private int m_Stage;                            // Multiplier stage
        private Coroutine m_MultiplierTick;             // Coroutine for multipliers tick
        private Coroutine m_PacketSpawn;                // Coroutine for spawning packets

        void Update()
        {
            m_Score += m_ScorePerSecond * m_Multiplier * Time.deltaTime;

            // Tick packets
            {
                float step = Time.deltaTime;// * m_Multiplier;

                for (int i = 0; i < m_DataPackets.activeCount; ++i)
                {
                    DataPacket packet = m_DataPackets.GetObject(i);
                    packet.TickPacket(step);
                }
            }

           // #if UNITY_EDITOR
                // Debug text
                if (m_DebugText)
                m_DebugText.text = string.Format("Score: {0}\nMultiplier: {1}\nMultiplier Stage: {2}\nPackets Pool Size: {3}\nPackets Active: {4}", 
                    Mathf.FloorToInt(m_Score), m_Multiplier, m_Stage, m_DataPackets.Count, m_DataPackets.activeCount);
           // #endif
        }

        /// <summary>
        /// Initialize manager to work with wire manager
        /// </summary>
        /// <param name="wireManager">Wire manager</param>
        public void Initialize(WireManager wireManager)
        {
            m_Score = 0f;
            m_Multiplier = 1f;
            m_Stage = 0;

            m_WireManager = wireManager;
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
            m_Score += (m_JumpScore * m_Multiplier);
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
        /// <returns>Packet or null</returns>
        private DataPacket GenerateRandomPacket()
        {
            const int maxAttempts = 5;
            Vector3 spawnCenter = m_WireManager.GetSpawnCircleCenter() + WireManager.WirePlane * (m_WireManager.segmentLength * m_MinPacketSpawnOffset);

            Vector3 position = Vector3.zero;
            bool success = false;

            // We don't want to loop to many times
            int attempts = 0;
            while (++attempts <= maxAttempts)
            {
                Vector2 circleOffset = m_WireManager.GetRandomSpawnCircleOffset();
                position = spawnCenter + new Vector3(circleOffset.x, circleOffset.y, 0f);

                success = m_WireManager.HasSpaceAtLocation(position);
                if (success)
                    break;
            }

            if (!success)
            {
                Debug.LogWarning(string.Format("Failed to generate packet after {0} attempts", maxAttempts), this);
                return null;
            }

            float speed = Random.Range(m_MinPacketSpeed, m_MaxPacketSpeed);

            return GeneratePacket(position, speed, m_PacketLifetime);
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
        /// Notify that player has collected given packet
        /// </summary>
        private void PacketCollected(DataPacket packet)
        {
            m_Score += (m_PacketScore * m_Multiplier);
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
        /// Routine for spawning packets
        /// </summary>
        private IEnumerator PacketSpawnRoutine()
        {
            while (enabled)
            {
                int delay = Random.Range(m_MinPacketSpawnInterval, m_MaxPacketSpawnInterval + 1);
                yield return new WaitForSegment(m_WireManager, m_WireManager.GetJumpersSegment() + delay);

                GenerateRandomPacket();
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
