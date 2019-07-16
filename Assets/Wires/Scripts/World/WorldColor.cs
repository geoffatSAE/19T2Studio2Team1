using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace TO5.Wires
{
    /// <summary>
    /// Handles changing of colors for world theme
    /// </summary>
    public class WorldColor : MonoBehaviour
    {
        [SerializeField] private PostProcessVolume m_Volume;        // Post-Process volume (assumed to be global)

        private Color m_BlendFrom, m_BlendTo;       // Colors to blend from and to
        private Material m_Skybox;                  // Skybox material
        private Grayscale m_Grayscale;              // Grayscale post processing effect

        private bool m_PulseEnabled = false;        // If grayscale pulse is enabled

        void Awake()
        {
            // We clone it as to not alter the original material
            m_Skybox = new Material(RenderSettings.skybox);
            RenderSettings.skybox = m_Skybox;

            if (m_Volume && m_Volume.profile)
            {
                m_Grayscale = m_Volume.profile.GetSetting<Grayscale>();
                m_Grayscale.enabled.value = false;
            }
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

        private IEnumerator GrayscalePulseRoutine()
        {
            if (m_Grayscale)
            {
                m_Grayscale.enabled.value = true;

                float start = Time.time;
                while (m_PulseEnabled)
                {
                    m_Grayscale.m_PulseTime.value = Time.time - start;
                    yield return null;
                }

                m_Grayscale.enabled.value = false;
            }
        }
    }
}
