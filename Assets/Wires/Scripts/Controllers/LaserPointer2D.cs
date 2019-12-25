using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TO5.Wires
{
    /// <summary>
    /// 2D implementation of the laser pointer to simulate a crosshair 
    /// </summary>
    public class LaserPointer2D : LaserPointerBase
    {
        [SerializeField] private Image m_Crosshair;     // Crosshair of the laser   

        /// <summary>
        /// Sets the position of the crosshair
        /// </summary>
        /// <param name="position">Position to set</param>
        public void SetCrosshairPosition(Vector2 position)
        {
            if (m_Crosshair)
                m_Crosshair.rectTransform.position = position;
        }

        // LaserPointerBase Interface
        protected override void SetLaserColor(Color color)
        {
            if (m_Crosshair)
                m_Crosshair.color = color;
        }
    }
}
