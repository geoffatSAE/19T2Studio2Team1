using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Laser pointer for helping players determine what they are pointing at.
    /// This function does not update itself automatically, must be called manually
    /// </summary>
    public class LaserPointer : MonoBehaviour
    {
        [Range(0.1f, 100f)] public float m_DefaultLaserLength = 100f;           // Default length of the laser (when nothing is hit)
        [SerializeField, Min(0.1f)] private float m_TraceRadius = 0.5f;         // Radius of sphere cast (larger radius will result in hit point being 'off' target)
        public LayerMask m_LayerMask = Physics.AllLayers;                       // Layers pointer can interact with

        public Color m_HitColor = Color.green;                                  // Color to set laser when hitting something
        public Color m_ClearColor = Color.white;                                // Color to set laser when hitting nothing
        public float m_ColorLerpSpeed = 2f;                                     // Speed of change between colors
        [SerializeField] private Transform m_Pivot;                             // Pivot of the laser pointer
        [SerializeField] private Renderer m_LaserRenderer;                      // Renderer of the laser
        [SerializeField] private Renderer m_LaserPointRenderer;                 // Renderer of the end point of the laser (when something is hit)       

        private bool m_HitSomething = false;            // If last trace hit something in the world
        private float m_LerpTime = 0f;                  // Time of lerp between colors (0 clear, 1 hit)

        void Awake()
        {
            if (m_LaserPointRenderer && m_LaserPointRenderer.material)
                m_LaserPointRenderer.material.color = m_HitColor;
        }

        void Update()
        {
            if (m_LaserRenderer && m_LaserRenderer.material)
            {
                float deltaTime = Time.deltaTime * m_ColorLerpSpeed * (m_HitSomething ? 1f : -1f);
                m_LerpTime = Mathf.Clamp01(m_LerpTime + deltaTime);

                Color laserColor = Color.Lerp(m_ClearColor, m_HitColor, m_LerpTime);
                m_LaserRenderer.material.color = laserColor;
            }
        }

        /// <summary>
        /// Points laser towards given direction (this should be called 
        /// </summary>
        /// <param name="origin">Origin of laser</param>
        /// <param name="direction">Direction of laser</param>
        public void PointLaser(Vector3 origin, Vector3 direction)
        {
            RaycastHit hit;
            Ray ray = new Ray(origin, direction);
            m_HitSomething = Physics.SphereCast(ray, m_TraceRadius, out hit, m_DefaultLaserLength, m_LayerMask);

            if (m_HitSomething)
                UpdateLaser(hit.distance, true);
            else
                UpdateLaser(m_DefaultLaserLength, true);

            if (m_LaserPointRenderer)
                m_LaserPointRenderer.enabled = m_HitSomething;
        }

        /// <summary>
        /// Updates lasers scale and position of hit point
        /// </summary>
        /// <param name="distance">Distance laser should be</param>
        /// <param name="movePoint">If hit point should be moved</param>
        private void UpdateLaser(float distance, bool movePoint)
        {
            // Scale laser
            if (m_Pivot && m_LaserRenderer)
            {
                // We scale Y as we expect pivot to be rotated
                Vector3 pivotScale = m_Pivot.localScale;
                pivotScale.y = distance * 0.5f;     // Small Hack, as for now we are using the default unity cylider mesh, which has a length of 1
                m_Pivot.localScale = pivotScale;
            }
            // We require pivot to properly set laser point
            else if (!m_Pivot)
            {
                movePoint = false;
            }

            // Move end point
            if (movePoint && m_LaserPointRenderer)
                m_LaserPointRenderer.transform.position = m_Pivot.position + transform.forward * distance;
        }
    }
}
