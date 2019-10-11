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

        // Total length of the wire
        public float length { get { return m_WireDistance; } }

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
            SetWireDistance(segments * segmentDistance);
            
            transform.position = start;

            m_Factory = factory;
            if (m_Factory)
                m_WireMesh.material.color = m_Factory.color;
        }

        /// <summary>
        /// Deactivates this wire from use
        /// </summary>
        public void DeactivateWire()
        {
            if (m_Spark)
            {
                m_Spark.DeactivateSpark();
                m_Spark = null;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets and activates the spark on this wire
        /// </summary>
        /// <param name="spark">Spark for this wire</param>
        /// <param name="onInterval">Interval for spark remaining on on state</param>
        /// <param name="offInterval">Interval for spark remaining on off state</param>
        public void ActivateSpark(Spark spark, float onInterval, float offInterval)
        {
            m_Spark = spark;
            m_Spark.ActivateSpark(this, onInterval, offInterval);
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

                m_Spark.TickSpark(step, progress);

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

        /// <summary>
        /// Adds segments to this wire
        /// </summary>
        /// <param name="segments">Value greater than zero</param>
        public void AddSegments(int segments)
        {
            if (segments > 0)
            {
                // Can't change length while player is jumping to this wire
                if (spark && spark.sparkJumper && spark.sparkJumper.isJumping)
                    return;

                m_Segments += segments;
                SetWireDistance(m_Segments * m_SegmentDistance);
            }
        }

        /// <summary>
        /// Sets the wires distance, updating mesh size and cached progress
        /// </summary>
        /// <param name="length">Length of wire</param>
        /// <param name="activation">If setting from activation</param>
        private void SetWireDistance(float length, bool activation = true)
        {
            if (length <= 0f)
                return;

            // Scale mesh to match length
            if (m_WireMesh)
            {
                // Need to reset this here so we can get accurate bounds (TODO: calculate in local space)
                m_Pivot.transform.localScale = Vector3.one;

                // Bounds is in world space (TODO: Should be using local space (which is Mesh.Bounds)
                Bounds meshBounds = m_WireMesh.bounds;
                float scaler = length / meshBounds.size.z;

                Vector3 scale = m_Pivot.localScale;
                scale.y = scaler;
                m_Pivot.localScale = scale;
            }

            if (activation)
            {
                m_WireDistance = length;
                return;
            }

            // Update progress so we remain where we should be (don't need to move spark)
            float newProgress = 0f;
            if (m_CachedProgress != 0f)
                newProgress = Mathf.Clamp01((m_WireDistance * m_CachedProgress) / length);

            // Only need to move spark if length is shorter than previous
            if (newProgress != m_CachedProgress && spark)
            {
                Transform sparkTransform = spark.transform;
                sparkTransform.position = transform.position + (WireManager.WirePlane * (m_WireDistance * m_CachedProgress));

                SparkJumper sparkJumper = spark.sparkJumper;
                if (sparkJumper)
                {
                    m_Spark.sparkJumper.SetPosition(sparkTransform.position);
                }
            }

            m_WireDistance = length;
            m_CachedProgress = newProgress;
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
