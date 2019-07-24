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
            m_Grayscale.enabled.Override(true);
            m_Grayscale.blend.Override(0f);

            m_Volume = PostProcessManager.instance.QuickVolume(gameObject.layer, 100f, m_Grayscale);
        }

        void OnDestroy()
        {
            RuntimeUtilities.DestroyVolume(m_Volume, true, false);
            Destroy(m_Grayscale);
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

        /// <summary>
        /// Set if grayscale post process effect should be enabled
        /// </summary>
        /// <param name="enable">Post poss process effect</param>
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
                float start = Time.time;
                while (m_PulseEnabled)
                {
                    if (m_GrayscalePulseSpeed > 0f)
                    {
                        // We don't use Mathf.Abs to allow grayscale to blend for a while
                        float alpha = Mathf.Max(0, Mathf.Cos((Time.time - start) * m_GrayscalePulseSpeed));

                        // Inversed as 'gray' is the default state
                        m_Grayscale.blend.value = Mathf.Lerp(0f, m_GrayscaleBlend, 1f - alpha);
                    }
                    else
                    {
                        m_Grayscale.blend.value = Mathf.Min(Time.time - start, m_GrayscaleBlend);
                    }

                    yield return null;
                }

                m_Grayscale.blend.value = 0f;
            }
        }
    }
}
