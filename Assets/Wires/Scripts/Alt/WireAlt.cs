using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    public class WireAlt : MonoBehaviour
    {
        // The spark on this wire
        public SparkAlt spark { get { return m_Spark; } }

        private SparkAlt m_Spark;       // Spark on this wire

        // If the spark jumper is on this wires spark
        public bool jumperAttached { get { return spark ? spark.sparkJumper != null : false; } }

        // The end of the wire in world space
        public Vector3 end { get { return transform.position + WireManager.WirePlane * m_WireDistance; } }

        private int m_Segments;             // Amount of segments on this wire
        private float m_SegmentDistance;    // Cahced distance per segment
        private float m_WireDistance;       // Cached total distance of wire

        public Transform m_Pivot;
        public Renderer m_WireMesh;

        /// <summary>
        /// Activates this wire, setting it's position and segments
        /// </summary>
        /// <param name="start">Starting position of this wire</param>
        /// <param name="segments">Amount of segments on this wire</param>
        /// <param name="segmentDistance">Distance of a segment</param>
        public void ActivateWire(Vector3 start, int segments, float segmentDistance)
        {
            gameObject.SetActive(true);

            m_Segments = segments;
            m_SegmentDistance = segmentDistance;
            m_WireDistance = segments * segmentDistance;

            transform.position = start;

            m_Pivot.transform.localScale = Vector3.one;

            // Bounds is in world space
            Bounds meshBounds = m_WireMesh.bounds;
            float scaler = m_WireDistance / meshBounds.size.z;

            Vector3 scale = m_Pivot.transform.localScale;
            scale.y = scaler;
            m_Pivot.transform.localScale = scale;
        }

        /// <summary>
        /// Deactivates this wire from use
        /// </summary>
        public void DeactivateWire()
        {
            if (m_Spark)
                m_Spark.DeactivateSpark();

            m_Spark = null;

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets and activates the spark on this wire
        /// </summary>
        /// <param name="spark">Spark for this wire</param>
        /// <param name="interval">Interval for spark switching jump state</param>
        public void ActivateSpark(SparkAlt spark, float interval)
        {
            m_Spark = spark;
            m_Spark.ActivateSpark(this, interval);
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
                progress = Mathf.Clamp01((Mathf.Abs(sparkTransform.position.z - transform.position.z) + step) / m_WireDistance);
                sparkTransform.position = transform.position + (WireManagerAlt.WirePlane * (m_WireDistance * progress));

                // Move the player with the spark if attached
                SparkJumperAlt sparkJumper = m_Spark.sparkJumper;
                if (sparkJumper != null && !sparkJumper.isJumping)
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
            Gizmos.DrawLine(transform.position, end);
        }
        #endif
    }
}
