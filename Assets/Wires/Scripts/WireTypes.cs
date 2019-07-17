using System;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Properties for how wires and sparks spawn and behave
    /// </summary>
    [Serializable]
    public class WireStageProperties
    {
        public float m_InnerSpawnRadius = 5f;                           // Inner radius of spawn circle (wires do not spawn inside this radius)
        public float m_OuterSpawnRadius = 20f;                          // Outer radius of spawn circle (wires do not spawn outside this radius)
        [Range(0, 1)] public float m_BottomCircleCutoff = 0.7f;         // Cutoff from bottom of spawn circle (no wires will spawn in cutoff)
        public int m_MinSegments = 8;                                   // Min segments per wire
        public int m_MaxSegments = 12;                                  // Max segments per wire
        public float m_MinSpawnInterval = 2f;                           // Min seconds between spawning wires
        public float m_MaxSpawnInterval = 3f;                           // Max seconds between spawning wires
        public int m_SpawnSegmentOffset = 5;                            // Offset from current segment to spawn wires
        [Min(0)] public int m_SpawnSegmentRange = 3;                    // Range from offset to spawn wires (between -Value and Value + 1)
        [Min(0)] public int m_SparkSpawnSegmentDelay = 3;               // Max segments to wait before spawning spark for wire
        [Range(0, 1)] public float m_DefectiveWireChance = 0.1f;        // Chance of wire be defective (0 for never, 1 for always)
        [Min(0)] public float m_SparkSpeed = 1f;                        // Speed of sparks    
        [Min(0)] public float m_SparkSwitchInterval = 1.5f;             // Interval for sparks switching on and off (0 for always on)
        [Min(0)] public float m_JumpTime = 0.75f;                       // Jump time for player
    }

    /// <summary>
    /// Properties for how data packets spawn and behave
    /// </summary>
    [Serializable]
    public class PacketStageProperties
    {
        public int m_MinSpawnOffset = 20;                               // Min segments in front of player to spawn
        public float m_MinSpawnInterval = 4;                            // Min seconds between spawning packets
        public float m_MaxSpawnInterval = 6;                            // Max seconds between spawning packets
        public float m_MinSpeed = 1f;                                   // Min speed of a packet
        public float m_MaxSpeed = 2.5f;                                 // Max speed of a packet
        [Min(0)] public float m_Lifetime = 10f;                         // How long data packets last for before expiring
        [Range(0, 1)] public float m_ClusterChance = 0.1f;              // Chance for packets spawning in a cluster (when spawn interval elapses)
        public int m_MinPacketsPerCluster = 3;                          // Min packets to try and spawn in a cluster
        public int m_MaxPacketsPerCluster = 6;                          // Max packets to try and spawn in a cluster
        public int m_ClusterRate = 5;                                   // Rates at which packet clusters happen (clusters only spawn after X attempts since the last) 
        [Min(0)] public int m_ClusterSpawnRange = 2;                    // Spawn range (in segments) for spawning clusters (m_MinPacketSpawnOffset + Random(-Range, Range))
    }
}
