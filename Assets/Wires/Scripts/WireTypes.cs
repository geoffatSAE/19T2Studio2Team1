using UnityEngine;
using UnityEngine.Profiling;

namespace TO5.Wires
{
    /// <summary>
    /// Properties for how wires and sparks spawn and behave
    /// </summary>
    [System.Serializable]
    public class WireStageProperties
    {
        public float m_InnerSpawnRadius = 5f;                           // Inner radius of spawn circle (wires do not spawn inside this radius)
        public float m_OuterSpawnRadius = 20f;                          // Outer radius of spawn circle (wires do not spawn outside this radius)
        [Range(0, 1)] public float m_BottomCircleCutoff = 0.7f;         // Cutoff from bottom of spawn circle (no wires will spawn in cutoff)
        [Range(0, 1)] public float m_TopCircleCutoff = 1f;              // Cutoff from top of spawn circle (no wires will spawn in cutoff)
        public int m_MaxWiresAtOnce = 5;                                // Max wires that can be active at once
        public int m_MinSegments = 8;                                   // Min segments per wire
        public int m_MaxSegments = 12;                                  // Max segments per wire
        public float m_MinSpawnInterval = 2f;                           // Min seconds between spawning wires
        public float m_MaxSpawnInterval = 3f;                           // Max seconds between spawning wires
        public int m_SpawnSegmentOffset = 5;                            // Offset from current segment to spawn wires
        [Min(0)] public int m_SpawnSegmentRange = 3;                    // Range from offset to spawn wires (between -Value and Value + 1)

        [Min(0)] public int m_SparkSpawnSegmentDelay = 3;               // Max segments to wait before spawning spark for wire
        [Range(0, 1)] public float m_DefectiveWireChance = 0.1f;        // Chance of wire be defective (0 for never, 1 for always)
        [Min(0)] public float m_SparkSpeed = 1f;                        // Speed of sparks  
        [Range(0, 1)] public float m_SparkDriftScale = 0.2f;            // Percentage of sparks speed to use when player is drifting (player drifts at same speed)
        [Min(0)] public float m_SparkOnSwitchInterval = 2f;             // Interval for sparks remaining on (0 means always on)
        [Min(0)] public float m_SparkOffSwitchInterval = 0.5f;          // Interval for sparks remaining off (0 means always off)

        // This is the easiest place to put these for now

        [Min(0)] public float m_JumpTime = 0.75f;                       // Jump time for player
    }

    /// <summary>
    /// Properties for how data packets spawn and behave
    /// </summary>
    [System.Serializable]
    public class PacketStageProperties
    {
        public int m_MinSpawnOffset = 20;                               // Min segments in front of player to spawn
        public float m_MinSpawnInterval = 4;                            // Min seconds between spawning packets
        public float m_MaxSpawnInterval = 6;                            // Max seconds between spawning packets
        public float m_MinSpeed = 1f;                                   // Min speed of a packet
        public float m_MaxSpeed = 2.5f;                                 // Max speed of a packet
        [Min(0)] public float m_Lifetime = 10f;                         // How long data packets last for before expiring (zero for do not spawn)

        [Range(0, 1)] public float m_ClusterChance = 0.1f;              // Chance for packets spawning in a cluster (when spawn interval elapses)
        public int m_MinPacketsPerCluster = 3;                          // Min packets to try and spawn in a cluster
        public int m_MaxPacketsPerCluster = 6;                          // Max packets to try and spawn in a cluster
        public int m_ClusterRate = 5;                                   // Rates at which packet clusters happen (clusters only spawn after X attempts since the last) 
        [Min(0)] public int m_ClusterSpawnRange = 2;                    // Spawn range (in segments) for spawning clusters (m_MinPacketSpawnOffset + Random(-Range, Range))

        // This is the easiet place to put these for now

        public float m_MultiplierIncreaseInterval = 15f;                // Time to reach next stage   
        public float m_HandicapMultiplierIncreaseInterval = 10f;        // Time to reach next stage after failing X times
    }

    /// <summary>
    /// Helper functions used by Wires
    /// </summary>
    struct Wires
    {
        private static float CutoffThreshold;       // Threshold for where random offset is most likely to loop for too long

        /// <summary>
        /// Generates a random offset of a circle (assuming origin is 0,0)
        /// </summary>
        /// <param name="minOffset">Min offset from origin</param>
        /// <param name="maxOffset">Max offset from origin</param>
        /// <param name="bottomCutoff">Cutoff from bottom</param>
        /// <param name="topCutoff">Cutoff from top</param>
        /// <returns>Random offset</returns>
        public static Vector2 GetRandomCircleOffset(float minOffset, float maxOffset, float bottomCutoff, float topCutoff)
        {
            // High chance of looping forever
            if (bottomCutoff <= CutoffThreshold && topCutoff <= CutoffThreshold)
            {
                Debug.LogWarning("Cutoffs are too close to zero");
                return Vector2.right * Random.Range(minOffset, maxOffset);
            }

            Profiler.BeginSample("GetRandomCircleOffset");

            float rad = Random.Range(0f, Mathf.PI * 2f);
            Vector2 direction = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));

            // We can quickly do one check instead of two if both are nearly the same
            if (Mathf.Approximately(bottomCutoff, topCutoff))
            {
                while (Mathf.Abs(Vector2.Dot(direction, Vector2.down)) > bottomCutoff)
                {
                    rad = Random.Range(0f, Mathf.PI * 2f);
                    direction.Set(Mathf.Sin(rad), Mathf.Cos(rad));
                }
            }
            else
            {
                while (Vector2.Dot(direction, Vector2.down) > bottomCutoff || Vector2.Dot(direction, Vector2.up) > topCutoff)
                {
                    rad = Random.Range(0f, Mathf.PI * 2f);
                    direction.Set(Mathf.Sin(rad), Mathf.Cos(rad));
                }
            }

            Profiler.EndSample();

            return direction * Random.Range(minOffset, maxOffset);
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Helper function for drawing spawning areas for wires and packets
        /// </summary>
        /// <param name="innerRadius">Areas inner radius</param>
        /// <param name="outerRadius">Areas outer radius</param>
        /// <param name="center">Center of area</param>
        public static void DrawSpawnArea(float innerRadius, float outerRadius, Vector3 center)
        {
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
                    Vector3 start = center + cdir * innerRadius;
                    Vector3 end = center + ndir * innerRadius;
                    Gizmos.DrawLine(start, end);
                }

                // Outer border
                {
                    Vector3 start = center + cdir * outerRadius;
                    Vector3 end = center + ndir * outerRadius;
                    Gizmos.DrawLine(start, end);
                }
            }
        }

        /// <summary>
        /// Helper function for drawing cutoff for spawning wires and packets
        /// </summary>
        /// <param name="cutoff">Cutoff value (between 0 and 1)</param>
        /// <param name="center">Center of circle</param>
        /// <param name="radius">Radius of circle</param>
        /// <param name="bottom">If drawing bottom or top cutoff</param>
        public static void DrawCutoffGizmo(float cutoff, Vector3 center, float radius, bool bottom)
        {
            float cutoffStart = Mathf.PI * (bottom ? 1.5f : 0.5f);
            float cutoffInverse = 1 - cutoff;

            // Left cutoff line
            {
                float rad = cutoffStart - (Mathf.PI * 0.5f * cutoffInverse);
                Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

                Gizmos.DrawLine(center, center + dir * radius);
            }

            // Right cutoff line
            {
                float rad = cutoffStart + (Mathf.PI * 0.5f * cutoffInverse);
                Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

                Gizmos.DrawLine(center, center + dir * radius);
            }
        }
        #endif
    }

}
