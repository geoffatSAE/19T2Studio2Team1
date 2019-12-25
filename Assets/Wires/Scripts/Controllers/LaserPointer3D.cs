using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// 3D implementation of the laser pointer to support VR gameplay
    /// </summary>
    public class LaserPointer3D : LaserPointerBase
    {
        [SerializeField] private Transform m_Pivot;                 // Pivot of the laser pointer
        [SerializeField] private Renderer m_LaserRenderer;          // Renderer of the laser
        [SerializeField] private Renderer m_LaserPointRenderer;     // Renderer of the end point of the laser (when something is hit)       

        // LaserPointerBase Interface
        protected override void SetLaserColor(Color color)
        {
            if (m_LaserRenderer)
                m_LaserRenderer.material.color = color;

            if (m_LaserPointRenderer)
                m_LaserPointRenderer.material.color = color;
        }

        public override void PointLaser(Vector3 origin, Vector3 direction)
        {
            base.PointLaser(origin, direction);

            if (m_LaserPointRenderer)
                m_LaserPointRenderer.enabled = m_HitSomething;
        }

        protected override void UpdateLaser(float distance)
        {
            // Scale laser
            if (m_Pivot)
            {
                // We scale Y as we expect pivot to be rotated
                Vector3 pivotScale = m_Pivot.localScale;
                pivotScale.y = distance * 0.5f;     // Small Hack, as for now we are using the default unity cylider mesh, which has a length of 1
                m_Pivot.localScale = pivotScale;

                // Move point
                if (m_LaserPointRenderer)
                    m_LaserPointRenderer.transform.position = m_Pivot.position + transform.forward * distance;
            }
        }
    }
}
