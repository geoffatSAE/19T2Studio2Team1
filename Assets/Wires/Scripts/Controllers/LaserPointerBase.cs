using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Base for laser pointer to help players aiming.
    /// This function does not update itself automatically. It must 
    /// be called manually using the PointLaser function
    /// </summary>
    public abstract class LaserPointerBase : MonoBehaviour
    {
        [Min(0.1f)] public float m_TraceLength = 100f;                          // Default length of laser (when nothing is hit)
        [SerializeField, Min(0.1f)] private float m_TraceRadius = 0.5f;         // Radius of sphere cast (larger radius will result in hit point being 'off' target)
        public LayerMask m_LayerMask = Physics.AllLayers;                       // Layers laser can interact with

        public Color m_HitColor = Color.green;                                  // Color to set laser when hitting something
        public Color m_ClearColor = Color.white;                                // Color to set laser when hitting nothing
        public float m_ColorLerpSpeed = 2f;                                     // Speed of change between colors

        protected bool m_HitSomething = false;              // If last trace hit something in the world
        protected float m_LerpTime = 0f;                    // Time of lerp between colors (0 clear, 1 hit)

        void Awake()
        {
            SetLaserColor(m_ClearColor);
        }

        void Update()
        {
            float deltaTime = m_ColorLerpSpeed * Time.deltaTime * (m_HitSomething ? 1f : -1f);
            m_LerpTime = Mathf.Clamp01(m_LerpTime + deltaTime);

            Color laserColor = Color.Lerp(m_ClearColor, m_HitColor, m_LerpTime);
            SetLaserColor(laserColor);
        }
        
        // Called when updating the color of the laser
        protected abstract void SetLaserColor(Color color);

        /// <summary>
        /// Points laser towards given direction. This
        /// must be called to properly update laser
        /// </summary>
        /// <param name="origin">Origin of laser</param>
        /// <param name="direction">Direction of laser</param>
        public virtual void PointLaser(Vector3 origin, Vector3 direction)
        {
            RaycastHit hit;
            Ray ray = new Ray(origin, direction);
            m_HitSomething = Physics.SphereCast(ray, m_TraceRadius, out hit, m_TraceLength, m_LayerMask);

            if (m_HitSomething)
                UpdateLaser(hit.distance);
            else
                UpdateLaser(m_TraceLength);
        }

        /// <summary>
        /// Updates laser based on distance to hit target.
        /// Distance will never be greater then m_TraceLength
        /// </summary>
        /// <param name="distance">Distance to hit point</param>
        protected virtual void UpdateLaser(float distance)
        {
            // Nothing by default
        }
    }
}
