using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    public class WireAlt : MonoBehaviour
    {
        private SparkAlt m_Spark;       // Spark on this wire

        private int m_Segments;             // Amount of segments on this wire
        private float m_SegmentDistance;    // Cahced distance per segment
        private float m_WireDistance;       // Cached total distance of wire

        /// <summary>
        /// Activates this wire, setting it's position and segments
        /// </summary>
        /// <param name="start">Starting position of this wire</param>
        /// <param name="segments">Amount of segments on this wire</param>
        /// <param name="segmentDistance">Distance of a segment</param>
        public void ActivateWire(Vector3 start, int segments, float segmentDistance)
        {
            m_Segments = segments;
            m_SegmentDistance = segmentDistance;
            m_WireDistance = segments * segmentDistance;

            transform.position = start;
        }

        /// <summary>
        /// Ticks this wire, moving the spark along
        /// </summary>
        /// <param name="step">Amount to move spark by</param>
        /// <returns>Percentage of wire that spark has completed</returns>
        public float TickWire(float step)
        {
            float progress = 0f;
            if (m_Spark && m_Spark.enabled)
            {
                Transform sparkTransform = m_Spark.transform;

                // Percentage of wire traversed
                progress = Mathf.Clamp01(Mathf.Abs(sparkTransform.position.x - transform.position.x) + step);
                sparkTransform.position = transform.position + (WireManagerAlt.WirePlane * (m_WireDistance * progress));

                // Move the player with the spark if attached
                if (m_Spark.sparkJumper != null)
                    m_Spark.sparkJumper.SetPosition(sparkTransform.position);
            }

            return progress;
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Called by wire manager to draw debug gizmos
        /// </summary>
        public void DrawDebugGizmos()
        {
            Gizmos.DrawLine(transform.position, transform.position + WireManager.WirePlane * m_WireDistance);
        }
        #endif
    }
}
