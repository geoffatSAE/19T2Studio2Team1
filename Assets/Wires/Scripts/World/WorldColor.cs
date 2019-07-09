using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Handles changing of colors for world theme
    /// </summary>
    public class WorldColor : MonoBehaviour
    {
        private Color m_BlendFrom, m_BlendTo;       // Colors to blend from and to
        private Material m_Skybox;                  // Skybox material

        void Awake()
        {
            // We clone it as to not alter the original material
            m_Skybox = new Material(RenderSettings.skybox);
            RenderSettings.skybox = m_Skybox;
        }

        /// <summary>
        /// Set active color to be portrayed, allowing BlendColors to be called
        /// </summary>
        /// <param name="color">Color to blend to</param>
        public void SetActiveColor(Color color)
        {
            m_BlendFrom = m_BlendTo;
            m_BlendTo = color;
        }

        /// <summary>
        /// Blends colors
        /// </summary>
        /// <param name="progress">Progress of blend</param>
        public void BlendColors(float progress)
        {
            progress = Mathf.Clamp01(progress);

            if (m_Skybox)
                m_Skybox.color = Color.Lerp(m_BlendFrom, m_BlendTo, progress);
        }
    }
}
