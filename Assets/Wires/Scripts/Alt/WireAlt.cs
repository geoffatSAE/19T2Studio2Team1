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
        /// Ticks this wire, moving the spark along
        /// </summary>
        /// <param name="deltaTime">Time to proceed by</param>
        /// <param name="speed">Speed of the game</param>
        /// <returns>Percentage of wire that spark has completed</returns>
        public float TickWire(float deltaTime, float speed)
        {
            float progress = 0f;
            if (m_Spark && m_Spark.enabled)
            {
                Vector3 offset = m_Spark.transform.position - transform.position;
                progress = offset.z / m_WireDistance;

                // TODO: Move spark
            }

            return progress;
        }
    }
}
