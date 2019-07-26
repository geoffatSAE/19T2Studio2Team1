using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TO5.Wires
{
    /// <summary>
    /// Handler for the tutorials UI
    /// </summary>
    public class TutorialUI : MonoBehaviour
    {
        [SerializeField] private Canvas m_Canvas;           // Canvas of tutorial
        [SerializeField] private RawImage m_Image;          // Image to manipulate
        [SerializeField] private Texture[] m_Slides;        // Textures for each slide
        [SerializeField] private Button m_SkipButton;       // Skip button

        private int m_SlideIndex = -1;      // Index of current slide

        // Button used to skip tutorial
        public Button skipButton { get { return m_SkipButton; } }

        /// <summary>
        /// Starts display slides
        /// </summary>
        public void StartSlides()
        {
            gameObject.SetActive(true);
            m_SlideIndex = -1;
        }

        /// <summary>
        /// Stops displaying slides
        /// </summary>
        public void EndSlides()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Moves to next slide
        /// </summary>
        public void NextSlide()
        {
            SetSlide(m_SlideIndex + 1);
        }

        /// <summary>
        /// Moves back to previous slide
        /// </summary>
        public void PreviousSlide()
        {
            SetSlide(m_SlideIndex - 1);
        }

        /// <summary>
        /// Sets the slide to display
        /// </summary>
        /// <param name="slide">Index of slide</param>
        private void SetSlide(int slide)
        {
            if (m_Image && m_Slides.Length > 0)
            {
                m_SlideIndex = Mathf.Clamp(slide, 0, m_Slides.Length - 1);
                m_Image.texture = m_Slides[m_SlideIndex];
            }
        }
    }
}
