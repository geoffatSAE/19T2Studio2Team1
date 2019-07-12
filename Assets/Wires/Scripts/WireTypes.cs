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
        public int m_MinSegments = 8;                                   // Min segments per wire
        public int m_MaxSegments = 12;                                  // Max segments per wire
        public float m_MinSpawnInterval = 2f;                           // Min seconds between spawning wires
        public float m_MaxSpawnInterval = 3f;                           // Max seconds between spawning wires
        public int m_SparkSpawnSegmentOffset = 5;                       // Offset from current segment to spawn wires
        [Min(0)] public int m_SparkSpawnSegmentRange = 3;               // Range from offset to spawn wires (between -Value and Value)
        [Min(0)] public int m_SparkSpawnSegmentDelay = 3;               // Max segments to wait before spawning spark for wire
        [Range(0, 1)] public float m_DefectiveWireChance = 0.1f;        // Chance of wire be defective (0 for never, 1 for always)
        [Min(0)] public float m_SparkSpeed = 1f;                        // Speed of sparks    
        [Min(0)] public float m_SparkSwitchInterval = 1.5f;             // Interval for sparks switching on and off (0 for always on)
    }

    /// <summary>
    /// Properties for how data packets spawn and behave
    /// </summary>
    [Serializable]
    public class PacketStageProperties
    {
        [SerializeField] public float m_MinSpawnInterval = 4;      // Min seconds between spawning packets
        [SerializeField] public float m_MaxSpawnInterval = 6;      // Max seconds between spawning packets
        [SerializeField] public float m_MinSpeed = 1f;             // Min speed of a packet
        [SerializeField] public float m_MaxSpeed = 2.5f;           // Max speed of a packet
        [SerializeField, Min(0)] public float m_Lifetime = 10f;    // How long data packets last for before expiring
    }
}
