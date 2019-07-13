﻿using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace TO5.Wires
{
    /// <summary>
    /// Handles changes of specific aesthetics for world theme
    /// </summary>
    public class WorldAesthetics : MonoBehaviour
    {
        [SerializeField] private Transform m_BorderPivot;               // Pivot for the outer wires border
        [SerializeField] private Renderer m_BorderRenderer;             // Renderer for the outer wires border
        [SerializeField] private Vector2[] m_BorderPanningSpeeds;       // Panning speeds for outer borders material for each multiplier stage
        private Material m_BorderMaterial;                              // Material of the outer wires border (expects _Color, _MainTex, _PanningSpeed and _AlphaScale)
        private float m_CachedBorderSize = 1f;                          // Cached size of border when starting

        [SerializeField] private ParticleSystem m_BorderParticles;      // Particle system for the outer wires particles
        [SerializeField] private float[] m_ParticleSimulationSpeeds;    // Velocity of particles for eahc multiplier stage
        private Color m_ParticleColor = Color.white;                    // Color of particles before blending

        private Wire m_ActiveWire;                              // Wire player is either on or travelling to
        private bool m_HaveSwitched = false;                    // If blend has switched (from old to new)

        void Awake()
        {
            if (m_BorderRenderer)
            {
                m_BorderMaterial = m_BorderRenderer.material;

                Bounds borderBounds = m_BorderRenderer.bounds;
                m_CachedBorderSize = borderBounds.size.z;

                // Don't render will we start
                m_BorderRenderer.enabled = false;
            }   

            if (m_BorderParticles)
            {
                ParticleSystem.TrailModule trails = m_BorderParticles.trails;
                m_ParticleColor = trails.colorOverLifetime.color;
            }
        }

        /// <summary>
        /// /// Sets active wire, instantly overriding current aesthetics
        /// </summary>
        /// <param name="wire"></param>
        public void SetActiveAesthetics(Wire wire)
        {
            if (wire)
            {
                m_ActiveWire = wire;
                UpdateBorderAesthetics(true);

                if (m_BorderRenderer)
                    m_BorderRenderer.enabled = true;
            }
        }

        /// <summary>
        /// Sets active wire to be blended to, allowing BlendAesthetics to be called
        /// </summary>
        /// <param name="wire">Wire to blend to</param>
        public void SetPendingAethetics(Wire wire)
        {
            if (wire)
            {
                m_ActiveWire = wire;
                m_HaveSwitched = false;

                if (m_BorderRenderer)
                    m_BorderRenderer.enabled = true;
            }
        }

        /// <summary>
        /// Set the intensity of the aesthetics
        /// </summary>
        /// <param name="intensity"></param>
        public void SetIntensity(int intensity)
        {
            if (m_BorderRenderer)
            {
                if (intensity < m_BorderPanningSpeeds.Length)
                {
                    Vector2 speed = m_BorderPanningSpeeds[intensity];
                    m_BorderMaterial.SetVector("_PanningSpeed", new Vector4(speed.x, speed.y, 0f, 0f));
                }
            }

            if (m_BorderParticles)
            {
                if (intensity < m_ParticleSimulationSpeeds.Length)
                {
                    float speed = m_ParticleSimulationSpeeds[intensity];

                    ParticleSystem.MainModule main = m_BorderParticles.main;
                    main.simulationSpeed = speed;
                }
            }
        }

        /// <summary>
        /// Blends aesthetics from old wire to new
        /// </summary>
        /// <param name="progress">Progress of blend</param>
        public void BlendAesthetics(float progress)
        {
            progress = Mathf.Clamp01(progress);

            bool switching = m_HaveSwitched;

            // Switch over while border should be finished
            if (!m_HaveSwitched && progress > 0.5f)
                UpdateBorderAesthetics(false);

            switching = switching != m_HaveSwitched;

            // Interpolate border
            if (m_BorderMaterial)
            {
                float alpha = 0f;
                if (!switching)
                    // y = x^2 (Parabola). We need to convert alpha from 0-1 to -1 to 1
                    alpha = Mathf.Pow((progress * 2f) - 1f, 2f);   

                m_BorderMaterial.SetFloat("_AlphaScale", alpha);
            }

            // Interpolate colors
            if (m_ActiveWire && m_ActiveWire.factory)
            {
                WireFactory factory = m_ActiveWire.factory;
                SetParticleColors(Color.Lerp(m_ParticleColor, factory.particleColor, progress));
            }
        }

        /// <summary>
        /// Updates the aethetics to match specifications on active wires factory
        /// </summary>
        /// <param name="particleColor">If particle colors should also be updated</param>
        private void UpdateBorderAesthetics(bool particleColor)
        {
            Assert.IsNotNull(m_ActiveWire);

            // Stretch outer wire
            if (m_BorderPivot && m_BorderRenderer)
            {
                Vector3 scale = m_BorderPivot.localScale;
                scale.y = m_ActiveWire.length / m_CachedBorderSize;
                m_BorderPivot.localScale = scale;

                m_BorderPivot.position = m_ActiveWire.transform.position;
            }

            WireFactory factory = m_ActiveWire.factory;
            if (!factory)
                return;

            // Update borders material properties
            if (m_BorderMaterial)
            {
                m_BorderMaterial.SetColor("_Color", factory.color);
                m_BorderMaterial.SetTexture("_MainTex", factory.borderTexture);
            }

            if (particleColor)
                SetParticleColors(factory.particleColor);

            m_HaveSwitched = true;
        }

        private void SetParticleColors(Color color)
        {
            if (m_BorderParticles)
            {
                ParticleSystem.MainModule main = m_BorderParticles.main;
                main.startColor = new ParticleSystem.MinMaxGradient(color);

                ParticleSystem.TrailModule trails = m_BorderParticles.trails;
                trails.colorOverLifetime = new ParticleSystem.MinMaxGradient(color);
                trails.colorOverTrail = new ParticleSystem.MinMaxGradient(color);
            }
        }
    }
}
