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
        /// <summary>
        /// Properties to set when player reaches new multiplier for flying packets
        /// </summary>
        [Serializable]
        public struct FlyingPacketsStage
        {
            public float m_SimulationSpeed;         // Speed of particle simulation
            public float m_SpawnRate;               // Spawn rate of particles
        }


        [Header("Border")]
        [SerializeField] private Transform m_BorderPivot;               // Pivot for the outer wires border
        [SerializeField] private Renderer m_BorderRenderer;             // Renderer for the outer wires border
        [SerializeField] private Vector2[] m_BorderPanningSpeeds;       // Panning speeds for outer borders material for each multiplier stage
        private Material m_BorderMaterial;                              // Material of the outer wires border (expects _Color, _MainTex, _PanningSpeed and _AlphaScale)
        private float m_CachedBorderSize = 1f;                          // Cached size of border when starting

        [Header("Zoom")]
        [SerializeField] private ParticleSystem m_BorderParticles;      // Particle system for the outer wires particles
        [SerializeField] private float[] m_ParticleSimulationSpeeds;    // Velocity of particles for each multiplier stage
        private Color m_ParticleColor = Color.white;                    // Color of particles before blending

        [Header("Warning")]
        [SerializeField] private Transform m_WarningPivot;              // Pivot for the end of wire sign

        [Header("Boost")]
        [SerializeField] private float m_BoostSpeedMultiplier = 1.5f;           // Amount to scale panning and simulation speed when boost is active
        [SerializeField] private ParticleSystem m_BoostActivationParticles;     // Particle system to play when boost is activated
        [SerializeField] private ParticleSystem m_BoostParticles;               // Particle system to play during boost     
        public float m_BoostParticlesSpeed = 2f;                                // Speed of boost particles
        public float m_BoostDissapateSpeed = 15f;                               // Speed at which boost particles disappear (when stopping due to jumping/drifting)
        private bool m_UpdateBoostParticles = false;                            // If boost particles need to be updated

        [Header("Flying Packets")]
        [SerializeField] private ParticleSystem m_FlyingPacketsParticles;       // Particle system emitting the flying packets
        [SerializeField] private FlyingPacketsStage[] m_FlyingPacketsStages;    // Properties to set flying packets per multiplier level

        [Header("Boost New")]
        [SerializeField] private ParticleSystem m_NewBoostParticles;
        [SerializeField] private FlyingPacketsStage[] m_NewBoostStages;

        private Wire m_ActiveWire;                              // Wire player is either on or travelling to
        private bool m_HaveSwitched = false;                    // If blend has switched (from old to new)
        private int m_Intensity = 0;                            // Intensity set last
        private bool m_BoostActive = false;                     // If boost is active

        private bool m_NewBoostParticlesActive = false;

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

                // Cache previous color now for blending
                if (m_ActiveWire && m_ActiveWire.factory)
                    m_ParticleColor = m_ActiveWire.factory.particleColor;

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
            float speedScale = m_BoostActive ? m_BoostSpeedMultiplier : 1f;

            if (m_BorderRenderer)
            {
                if (intensity < m_BorderPanningSpeeds.Length)
                {
                    Vector2 speed = m_BorderPanningSpeeds[intensity] * speedScale;
                    m_BorderMaterial.SetVector("_PanningSpeed", new Vector4(speed.x, speed.y, 0f, 0f));
                }
            }

            if (m_BorderParticles)
            {
                if (intensity < m_ParticleSimulationSpeeds.Length)
                {
                    float speed = m_ParticleSimulationSpeeds[intensity] * speedScale;

                    ParticleSystem.MainModule main = m_BorderParticles.main;
                    main.simulationSpeed = speed;
                }
            }

            if (m_NewBoostParticles)
            {
                if (intensity < m_NewBoostStages.Length)
                {
                    FlyingPacketsStage props = m_NewBoostStages[intensity];

                    if (props.m_SimulationSpeed > 0f)
                    {
                        m_NewBoostParticles.gameObject.SetActive(true);
                        m_NewBoostParticles.Play();

                        ParticleSystem.MainModule main = m_FlyingPacketsParticles.main;
                        main.simulationSpeed = props.m_SimulationSpeed;

                        ParticleSystem.EmissionModule emission = m_FlyingPacketsParticles.emission;
                        emission.rateOverTime = props.m_SpawnRate;

                        m_NewBoostParticlesActive = true;
                    }
                    else
                    {
                        m_NewBoostParticles.gameObject.SetActive(false);
                        m_NewBoostParticles.Stop();

                        m_NewBoostParticlesActive = false;
                    }
                }
            }

            SetFlyingPacketsStage(intensity);

            m_Intensity = intensity;
        }

        /// <summary>
        /// Set if aesthetics should be boosted
        /// </summary>
        /// <param name="active">Activate boosted aesthetics</param>
        public void SetBoostActive(bool active)
        {
            if (m_BoostActive != active)
            {
                m_BoostActive = active;
                SetIntensity(m_Intensity);
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

            // Place warning
            if (m_WarningPivot)
            {
                m_WarningPivot.gameObject.SetActive(true);
                m_WarningPivot.position = m_ActiveWire.end;
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

            if (m_UpdateBoostParticles)
                SetBoostColor(factory.boostColor);

            m_HaveSwitched = true;
        }

        /// <summary>
        /// Sets the color for the border particles
        /// </summary>
        /// <param name="color">Color to use</param>
        private void SetParticleColors(Color color)
        {
            if (m_BorderParticles)
            {
                ParticleSystem.MainModule main = m_BorderParticles.main;
                main.startColor = color;

                ParticleSystem.TrailModule trails = m_BorderParticles.trails;
                trails.colorOverLifetime = color;
                trails.colorOverTrail = color;
            }

            if (m_NewBoostParticles)
            {
                ParticleSystem.MainModule main = m_NewBoostParticles.main;
                main.startColor = color;

                ParticleSystem.TrailModule trails = m_NewBoostParticles.trails;
                trails.colorOverLifetime = color;
                trails.colorOverTrail = color;
            }
        }

        /// <summary>
        /// Set if warning sign should be displayed
        /// </summary>
        /// <param name="enable">Display sign</param>
        public void SetWarningSignEnabled(bool enable)
        {
            if (m_WarningPivot)
                m_WarningPivot.gameObject.SetActive(enable);
        }

        /// <summary>
        /// Set if particles for boost should be enabled
        /// </summary>
        /// <param name="enable">Enable particles</param>
        /// <param name="speed">Speed of simulation</param>
        public void SetBoostParticlesEnabled(bool enable, float speed)
        {
            //if (m_UpdateBoostParticles != enable)
            {
                m_UpdateBoostParticles = enable;

                //// These play upon activation
                //if (m_BoostActivationParticles)
                //{
                //    if (m_BoostActivationParticles.IsAlive())
                //        m_BoostActivationParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                //    if (m_UpdateBoostParticles)
                //    {
                //        WireFactory factory = m_ActiveWire ? m_ActiveWire.factory : null;
                //        if (factory)
                //        {
                //            ParticleSystem.TrailModule trails = m_BoostActivationParticles.trails;
                //            trails.colorOverLifetime = factory.boostColor;
                //        }

                //        m_BoostActivationParticles.Play();
                //    }
                //}

                //// These play continuously
                //if (m_BoostParticles)
                //{
                //    ParticleSystem.MainModule main = m_BoostParticles.main;
                //    main.simulationSpeed = speed;

                //    if (m_UpdateBoostParticles)
                //    {
                //        WireFactory factory = m_ActiveWire ? m_ActiveWire.factory : null;
                //        if (factory)
                //        {
                //            ParticleSystem.TrailModule trails = m_BoostParticles.trails;
                //            trails.colorOverLifetime = factory.boostColor;
                //        }

                //        m_BoostParticles.Play();
                //    }
                //    else
                //    {
                //        m_BoostParticles.Stop();
                //    }
                //}

                if (m_NewBoostParticles)
                {
                    if (enable)
                    {
                        if (m_NewBoostParticlesActive)
                        {
                            m_NewBoostParticles.Play();
                        }
                    }
                    else
                    {
                        m_NewBoostParticles.Stop();
                    }
                }
            }
        }

        /// <summary>
        /// Sets the color for the boost particles
        /// </summary>
        /// <param name="color">Color to use</param>
        private void SetBoostColor(ParticleSystem.MinMaxGradient color)
        {
            if (m_BoostParticles)
            {
                ParticleSystem.TrailModule trails = m_BoostParticles.trails;
                trails.colorOverLifetime = color;
            }
        }

        /// <summary>
        /// Sets the flying packet stage properties to use
        /// </summary>
        /// <param name="index">Index of stage</param>
        private void SetFlyingPacketsStage(int index)
        {
            if (m_FlyingPacketsParticles)
            {
                if (index < m_FlyingPacketsStages.Length)
                {
                    FlyingPacketsStage stage = m_FlyingPacketsStages[index];

                    ParticleSystem.MainModule main = m_FlyingPacketsParticles.main;
                    main.simulationSpeed = stage.m_SimulationSpeed;

                    ParticleSystem.EmissionModule emission = m_FlyingPacketsParticles.emission;
                    emission.rateOverTime = stage.m_SpawnRate;
                }
            }
        }
    }
}
