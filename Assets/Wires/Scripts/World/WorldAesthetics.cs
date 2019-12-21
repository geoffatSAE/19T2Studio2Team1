using System;
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
        /// Properties to set when player reaches new multiplier for certain particles
        /// </summary>
        [Serializable]
        public struct ParticleStageProperties
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
        private Color m_BorderColor = Color.white;                      // Color of border before blending

        [Header("Zoom")]
        [SerializeField] private ParticleSystem m_BorderParticles;      // Particle system for the outer wires particles
        [SerializeField] private float[] m_ParticleSimulationSpeeds;    // Velocity of particles for each multiplier stage
        private Color m_ParticleColor = Color.white;                    // Color of particles before blending

        [Header("Warning")]
        [SerializeField] private Transform m_WarningPivot;              // Pivot for the end of wire sign
        [SerializeField] private Renderer m_WarningSignRenderer;        // Renderer for the warning sign
        [SerializeField] private Texture2D m_UnlockedWireWarning;       // Texture to use when wire can be extended
        [SerializeField] private Texture2D m_LockedWireWarning;         // Texture to use when wire can't be extended

        private bool m_WarningSignLocked = false;                       // If warning sign is locked in an off state

        [Header("Boost")]
        [SerializeField] private float m_BoostSpeedMultiplier = 1.5f;           // Amount to scale panning and simulation speed when boost is active
        [SerializeField] private ParticleSystem m_BoostActivationParticles;     // Particle system to play when boost is activated
        [SerializeField] private ParticleSystem m_BoostParticles;               // Particle system to play during boost     
        [SerializeField] private ParticleStageProperties[] m_BoostStages;       // Properties to set boost particles per multiplier level
        public float m_BoostDissapateSpeed = 15f;                               // Speed at which boost particles disappear (when stopping due to jumping/drifting)

        [NonSerialized] public float m_BoostParticlesSpeed = 0f;                // Current speed of the boost particles
        private bool m_EnableBoostParticles = false;                            // If boost particles should be enabled
        private bool m_UpdateBoostParticles = true;                             // If boost particles need to be updated     

        [Header("Flying Packets")]
        [SerializeField] private ParticleSystem m_FlyingPacketsParticles;               // Particle system emitting the flying packets
        [SerializeField] private ParticleStageProperties[] m_FlyingPacketsStages;       // Properties to set flying packets per multiplier level

        private Wire m_ActiveWire;                              // Wire player is either on or travelling to 
        private bool m_HaveSwitched = false;                    // If blend has switched (from old to new)
        private bool m_BoostActive = false;                     // If boost is active

        private TunnelVision m_TunnelInstance;                  // Tunnel instance we can also update

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

            SetWarningSignTexture(m_UnlockedWireWarning);
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
                // Cache previous color now for blending
                if (m_ActiveWire && m_ActiveWire.factory)
                {
                    m_BorderColor = m_ActiveWire.factory.color;
                    m_ParticleColor = m_ActiveWire.factory.particleColor;

                    if (m_TunnelInstance)
                    {
                        Color color = m_BorderColor;
                        color.a = 0f;
                        m_TunnelInstance.SetColor(color);
                        m_TunnelInstance.SetTextures(m_ActiveWire.factory.borderTexture, wire.factory.borderTexture);
                    }
                }

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

            if (m_BoostParticles)
            {
                // This will handle turning particles on/off
                SetBoostStage(intensity);
            }

            SetFlyingPacketsStage(intensity);
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

                if (m_TunnelInstance)
                {
                    Color color = Color.Lerp(m_BorderColor, factory.color, progress);
                    color.a = progress;
                    m_TunnelInstance.SetColor(color);
                }
            }
        }

        /// <summary>
        /// Refreshes the outer border size and warning sign placement.
        /// This should be used when adding segments to the active wire 
        /// </summary>
        public void RefreshWireBasedAesthetics()
        {
            if (!m_ActiveWire)
                return;

            // Stretch outer wire
            if (m_BorderPivot && m_BorderRenderer)
            {
                Vector3 scale = m_BorderPivot.localScale;
                scale.y = m_ActiveWire.length / m_CachedBorderSize;
                m_BorderPivot.localScale = scale;

                m_BorderPivot.position = m_ActiveWire.transform.position;
            }

            // Place warning
            if (m_WarningPivot && !m_WarningSignLocked)
            {
                m_WarningPivot.gameObject.SetActive(true);
                m_WarningPivot.position = m_ActiveWire.end;
            }
        }

        /// <summary>
        /// Updates the aethetics to match specifications on active wires factory
        /// </summary>
        /// <param name="particleColor">If particle colors should also be updated</param>
        private void UpdateBorderAesthetics(bool particleColor)
        {
            Assert.IsNotNull(m_ActiveWire);

            RefreshWireBasedAesthetics();

            SetWarningSignedLocked(false);

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

            if (m_TunnelInstance)
            {
                Color color = factory.color;
                color.a = 1f;
                m_TunnelInstance.SetColor(color);
                m_TunnelInstance.SetTextures(factory.borderTexture, factory.borderTexture);
            }

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
        }

        /// <summary>
        /// Set if warning sign should be displayed
        /// </summary>
        /// <param name="enable">Display sign</param>
        public void SetWarningSignEnabled(bool enable)
        {
            if (m_WarningPivot && !m_WarningSignLocked)
                m_WarningPivot.gameObject.SetActive(enable);
        }

        /// <summary>
        /// Set if warning sign is toggable or not
        /// </summary>
        /// <param name="locked">If sign is locked in an off state</param>
        public void LockWarningSign(bool locked)
        {
            if (locked != m_WarningSignLocked)
            {
                m_WarningSignLocked = locked;
                if (m_WarningSignLocked && m_WarningPivot)
                    m_WarningPivot.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Set if particles for boost should be enabled
        /// </summary>
        /// <param name="enable">Enable particles</param>
        /// <param name="speed">Speed of simulation</param>
        public void SetBoostParticlesEnabled(bool enable, float speed)
        {
            if (enable != m_UpdateBoostParticles)
            {
                m_UpdateBoostParticles = enable;

                if (!m_EnableBoostParticles)
                    return;

                if (m_UpdateBoostParticles)
                {
                    WireFactory factory = m_ActiveWire ? m_ActiveWire.factory : null;
                    if (factory)
                        SetBoostColor(factory.boostColor);
                }

                // These play upon activation
                if (m_BoostActivationParticles)
                {
                    if (m_BoostActivationParticles.IsAlive())
                        m_BoostActivationParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                    if (m_UpdateBoostParticles)
                        m_BoostActivationParticles.Play();
                }

                // These play continuously
                if (m_BoostParticles)
                {
                    ParticleSystem.MainModule main = m_BoostParticles.main;
                    main.simulationSpeed = speed;

                    if (m_UpdateBoostParticles)
                        m_BoostParticles.Play();
                    else
                        m_BoostParticles.Stop();
                }
            }
        }

        /// <summary>
        /// Sets the color for the boost particles
        /// </summary>
        /// <param name="color">Color to use</param>
        private void SetBoostColor(ParticleSystem.MinMaxGradient color)
        {
            if (m_BoostActivationParticles)
            {
                ParticleSystem.TrailModule trails = m_BoostActivationParticles.trails;
                trails.colorOverLifetime = color;
            }

            if (m_BoostParticles)
            {
                ParticleSystem.TrailModule trails = m_BoostParticles.trails;
                trails.colorOverLifetime = color;
            }
        }

        /// <summary>
        /// Sets the boost stage properties to use
        /// </summary>
        /// <param name="index">Index of stage</param>
        private void SetBoostStage(int index)
        {
            if (m_BoostParticles)
            {
                if (index < m_BoostStages.Length)
                {
                    ParticleStageProperties props = m_BoostStages[index];
                    m_EnableBoostParticles = props.m_SimulationSpeed > 0f;

                    if (m_EnableBoostParticles)
                    {
                        SetParticlesStage(m_BoostParticles, props);

                        // We don't want to start playing the boost if currently jumping
                        if (m_UpdateBoostParticles)
                        {
                            if (m_ActiveWire && m_ActiveWire.factory)
                                SetBoostColor(m_ActiveWire.factory.color);

                            if (m_BoostActivationParticles)
                                m_BoostActivationParticles.Play();

                            m_BoostParticles.Play();
                        }       
                    }
                    else if (m_BoostParticles.isPlaying)
                    {
                        m_BoostParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }

                    m_BoostParticlesSpeed = props.m_SimulationSpeed;
                }
            }
        }

        /// <summary>
        /// Sets the flying packet stage properties to use
        /// </summary>
        /// <param name="index">Index of stage</param>
        private void SetFlyingPacketsStage(int index)
        {
            if (index < m_FlyingPacketsStages.Length)
                SetParticlesStage(m_FlyingPacketsParticles, m_FlyingPacketsStages[index]);
        }

        /// <summary>
        /// Helper function for setting certain particle module properties
        /// </summary>
        /// <param name="system">Particle system to modify</param>
        /// <param name="props">Properties to set</param>
        private void SetParticlesStage(ParticleSystem system, ParticleStageProperties props)
        {
            if (system)
            {
                ParticleSystem.MainModule main = system.main;
                main.simulationSpeed = props.m_SimulationSpeed;

                ParticleSystem.EmissionModule emission = system.emission;
                emission.rateOverTime = props.m_SpawnRate;
            }
        }

        /// <summary>
        /// Sets the optional tunnel vision script to also update.
        /// </summary>
        /// <param name="tunnelVision">Tunnel Vision script to update (Can be null to reset)</param>
        public void SetTunnelVision(TunnelVision tunnelVision)
        {
            m_TunnelInstance = tunnelVision;
            if (m_TunnelInstance)
            {
                WireFactory wireFactory = m_ActiveWire ? m_ActiveWire.factory : null;
                if (!wireFactory)
                    return;

                Color tunnelColor = wireFactory.color;
                tunnelColor.a = 1f;

                m_TunnelInstance.SetColor(tunnelColor);
                m_TunnelInstance.SetTextures(wireFactory.borderTexture, wireFactory.borderTexture);
            }
        }

        /// <summary>
        /// Set the warning sign texture based on if the wire is locked (from increasing in size)
        /// </summary>
        /// <param name="locked">If to use locked texture</param>
        public void SetWarningSignedLocked(bool locked)
        {
            SetWarningSignTexture(locked ? m_LockedWireWarning : m_UnlockedWireWarning);
        }

        /// <summary>
        /// Updates the texture of the warning sign
        /// </summary>
        /// <param name="texture">Texture to set</param>
        private void SetWarningSignTexture(Texture2D texture)
        {
            if (m_WarningSignRenderer)
                m_WarningSignRenderer.material.mainTexture = texture;
        }
    }
}
