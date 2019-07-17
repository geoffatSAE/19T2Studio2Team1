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
        [SerializeField, Range(0, 1)] private float m_GrayscaleBlend = 0.8f;
        [SerializeField] private float m_GrayscalePulseSpeed = 1f;

        private Color m_BlendFrom, m_BlendTo;       // Colors to blend from and to
        private Material m_Skybox;                  // Skybox material

        private PostProcessVolume m_Volume;         // Volume for post processing
        private Grayscale m_Grayscale;              // Grayscale post processing effect
        private bool m_PulseEnabled = false;        // If grayscale pulse is enabled

        void Awake()
        {
            // We clone it as to not alter the original material
            m_Skybox = new Material(RenderSettings.skybox);
            RenderSettings.skybox = m_Skybox;

            m_Grayscale = ScriptableObject.CreateInstance<Grayscale>();
            m_Grayscale.enabled.Override(false);
            m_Grayscale.blend.Override(0f);

            m_Volume = PostProcessManager.instance.QuickVolume(0, 1f, m_Grayscale);
            m_Volume.enabled = false;
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

        public void SetGrayscaleEnabled(bool enable)
        {
            if (m_PulseEnabled != enable)
            {
                m_PulseEnabled = enable;

                // This routine will exit itself upon completion
                if (m_PulseEnabled)
                    StartCoroutine(GrayscalePulseRoutine());
            }
        }

        /// <summary>
        /// Routine for pulsing the grayscale of the scene
        /// </summary>
        private IEnumerator GrayscalePulseRoutine()
        {
            if (m_Grayscale)
            {
                m_Volume.enabled = true;
                m_Grayscale.enabled.Override(true);

                float start = Time.time;
                while (m_PulseEnabled)
                {
                    float alpha = Mathf.Abs(Mathf.Sin((Time.time - start) * m_GrayscalePulseSpeed));
                    m_Grayscale.blend.Override(Mathf.Lerp(0f, m_GrayscaleBlend, alpha));

                    yield return null;
                }

                m_Grayscale.enabled.Override(false);
                m_Volume.enabled = false;
            }
        }
    }
}
