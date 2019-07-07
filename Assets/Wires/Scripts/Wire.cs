using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    public class Wire : MonoBehaviour
    {
        // The spark on this wire
        public Spark spark { get { return m_Spark; } }

        // Progress of spark on this wire
        public float sparkProgress { get { return m_CachedProgress; } }

        // If the spark jumper is on this wires spark
        public bool jumperAttached { get { return spark ? spark.sparkJumper != null : false; } }

        private Spark m_Spark;              // Spark on this wire
        private float m_CachedProgress;     // Cached progress from last tick

        // The end of the wire in world space
        public Vector3 end { get { return transform.position + WireManager.WirePlane * m_WireDistance; } }

        // Factory associated with this wire
        public WireFactory factory { get { return m_Factory; } }

        private int m_Segments;             // Amount of segments on this wire
        private float m_SegmentDistance;    // Cahced distance per segment
        private float m_WireDistance;       // Cached total distance of wire
        private WireFactory m_Factory;      // Factory for this wires aesthetics
        
        public Transform m_Pivot;
        public Renderer m_WireMesh;
        public Renderer m_BorderMesh;

        /// <summary>
        /// Activates this wire, setting it's position and segments
        /// </summary>
        /// <param name="start">Starting position of this wire</param>
        /// <param name="segments">Amount of segments on this wire</param>
        /// <param name="segmentDistance">Distance of a segment</param>
        /// <param name="factory">Factory for wire to use</param>
        public void ActivateWire(Vector3 start, int segments, float segmentDistance, WireFactory factory)
        {
            gameObject.SetActive(true);

            m_Segments = segments;
            m_SegmentDistance = segmentDistance;
            m_WireDistance = segments * segmentDistance;
            m_Factory = factory;

            transform.position = start;

            m_Pivot.transform.localScale = Vector3.one;

            if (m_WireMesh)
            {
                // Bounds is in world space
                Bounds meshBounds = m_WireMesh.bounds;
                float scaler = m_WireDistance / meshBounds.size.z;

                Vector3 scale = m_Pivot.transform.localScale;
                scale.y = scaler;
                m_Pivot.transform.localScale = scale;
            }

            if (m_BorderMesh && factory)
            {
                Color color = factory.color;
                color.a = 0.3f;
                m_BorderMesh.material.color = color;

                m_BorderMesh.gameObject.SetActive(false);

                m_WireMesh.material.color = factory.color;
            }
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
        public void ActivateSpark(Spark spark, float interval)
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
                sparkTransform.position = transform.position + (WireManager.WirePlane * (m_WireDistance * progress));

                // Move the player with the spark if attached
                SparkJumper sparkJumper = m_Spark.sparkJumper;
                if (sparkJumper != null && !sparkJumper.isJumping)
                {
                    m_Spark.sparkJumper.SetPosition(sparkTransform.position);
                    m_Spark.sparkJumper.wireDistanceTravelled += step;
                }       
            }

            m_CachedProgress = progress;
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
