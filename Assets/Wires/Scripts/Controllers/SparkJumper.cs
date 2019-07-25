﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{ 
    /// <summary>
    /// Controller for the player, can not be instanced by itself.
    /// Provides core functionallity for player interaction
    /// </summary>
    public abstract class SparkJumper : MonoBehaviour
    {
        /// <summary>
        /// Delegate for when jumper is jumping to new spark (both starting and ending)
        /// </summary>
        /// <param name="spark">Spark that has been jumped to (Can be null)</param>
        /// <param name="finished">If call is when jump has finished</param>
        public delegate void JumpedToSpark(Spark spark, bool finished);

        /// <summary>
        /// Delegate for when jumper enters/exits drifting mode
        /// </summary>
        /// <param name="isEnabled">If jumper is now drifting</param>
        public delegate void DriftingUpdated(bool isEnabled);

        /// <summary>
        /// Delegate for when jumper is attempting to activate the boost
        /// </summary>
        /// <returns>If activation was successful</returns>
        public delegate bool TryActivateBoost();

        public JumpedToSpark OnJumpToSpark;             // Event for when jumping to new spark
        public DriftingUpdated OnDriftingUpdated;       // Event for when entering/exiting drifting
        public TryActivateBoost OnActivateBoost;        // Event for when attempting boost activation

        public Transform m_Anchor;                                                          // Anchor to move instead of gameObject
        public bool m_JumpingEnabled = true;                                                // If player is allowed to jump
        public float m_JumpTime = 0.75f;                                                    // Transition time between sparks
        [SerializeField, Min(0.1f)] protected float m_TraceRadius = 0.5f;                   // Radius of sphere cast
        [SerializeField] private LayerMask m_InteractiveLayer = Physics.AllLayers;          // Layer for interactives
        public CompanionUI m_Companion;                                                     // Players companion

        public ScreenFade m_ScreenFade;                     // Screen fade for game transitions
        public AudioSource m_SelectionAudioSource;          // Audio source for playing selection sounds

        // If the player is allowed to request a jump
        public bool canJump { get { return m_JumpingEnabled && !m_IsJumping; } }

        // If the player is drifting in space
        public bool isDrifting { get { return m_IsDrifting; } }

        // Spark the player is on
        public Spark spark { get { return m_Spark; } }

        // Wire the player is on
        public Wire wire { get { return m_Spark ? m_Spark.GetWire() : null; } }

        // Progress along players current wire
        public float wireProgress { get { return m_Spark ? m_Spark.GetWire().sparkProgress : 0f; } }

        // If player is jumping
        public bool isJumping { get { return m_IsJumping; } }

        // Progress of jump
        public float jumpProgress { get { return m_IsJumping ? m_CachedJumpProgress : 0f; } }

        // Distance travelled on wires
        public float wireDistanceTravelled { get { return m_WireDistanceTravelled; } set { m_WireDistanceTravelled = value; } }

        private Spark m_Spark;                          // Spark we are on
        private bool m_IsJumping = false;               // If transition is in progress
        private bool m_IsDrifting = false;              // If drifting in space
        private float m_CachedJumpProgress = -1f;       // Cached progress of jump
        private float m_WireDistanceTravelled = 0f;     // Distance jumper has travelled while on wires

        /// <summary>
        /// Jumps to given spark
        /// </summary>
        /// <param name="spark">Spark to jump to</param>
        public void JumpToSpark(Spark spark, bool force = false)
        {
            if (!spark || m_IsJumping || !spark.canJumpTo && !force)
                return;

            if (m_Spark)
                m_Spark.DetachJumper();

            m_Spark = spark;

            m_Spark.FreezeSwitching();
            m_Spark.AttachJumper(this);

            // Can't be drifting while jumping
            SetDriftingEnabled(false);

            StartCoroutine(JumpRoutine());

            if (OnJumpToSpark != null)
                OnJumpToSpark(m_Spark, false);           
        }

        /// <summary>
        /// Instantly jumps to spark (even if spark.canJumpTo is false). This does not call OnJumpToSpark
        /// </summary>
        /// <param name="spark">Spark to jump to</param>
        public void InstantJumpToSpark(Spark spark)
        {
            if (!spark)
                return;

            if (m_Spark)
                m_Spark.DetachJumper();

            m_Spark = spark;

            m_IsJumping = false;
            SetDriftingEnabled(false);

            m_Spark.FreezeSwitching();
            m_Spark.AttachJumper(this);
        }

        /// <summary>
        /// Jumps off of current spark
        /// </summary>
        public void JumpOffSpark()
        {
            if (m_Spark)
            {
                m_Spark.DetachJumper();
                m_Spark = null;
            }
        }

        /// <summary>
        /// Traces the world for interactives
        /// </summary>
        /// <param name="origin">Origin of the trace</param>
        /// <param name="direction">Direction of the trace</param>
        public void TraceWorld(Vector3 origin, Quaternion direction)
        {
            TraceWorld(new Ray(origin, direction * Vector3.forward));
        }

        /// <summary>
        /// Traces the world for interactives
        /// </summary>
        /// <param name="ray">Ray of the trace</param>
        public void TraceWorld(Ray ray)
        {
            //if (!canJump)
            //    return;

            RaycastHit hit;
            if (Physics.SphereCast(ray, m_TraceRadius, out hit, Mathf.Infinity, m_InteractiveLayer))
            {
                GameObject hitObject = hit.collider.gameObject;

                // We only check for interactives
                IInteractive interactive = hitObject.GetComponent<IInteractive>();
                if (interactive != null && interactive.CanInteract(this))
                    interactive.OnInteract(this);      
            }
        }

        /// <summary>
        /// Attempts to activate boost
        /// </summary>
        /// <returns>If boost was activated</returns>
        public bool ActivateBoost()
        {
            if (m_IsDrifting)
                return false;

            if (OnActivateBoost != null)
                return OnActivateBoost.Invoke();

            return false;
        }

        /// <summary>
        /// Set if jumper is drifting in space
        /// </summary>
        /// <param name="enable">If drifting</param>
        public void SetDriftingEnabled(bool enable)
        {
            if (m_IsDrifting != enable)
            {
                m_IsDrifting = enable;

                if (OnDriftingUpdated != null)
                    OnDriftingUpdated.Invoke(m_IsDrifting);
            }
        }

        /// <summary>
        /// Sets the jumpers position, using anchor if set
        /// </summary>
        /// <param name="position">Position of jumper</param>
        public void SetPosition(Vector3 position)
        {
            if (m_Anchor)
                m_Anchor.position = position;
            else
                transform.position = position;
        }

        /// <summary>
        /// Get the jumpers position, using anchor if set
        /// </summary>
        /// <returns>Position of jumper</returns>
        public Vector3 GetPosition()
        {
            if (m_Anchor)
                return m_Anchor.position;
            else
                return transform.position;
        }

        /// <summary>
        /// Get the players actual position in the world
        /// </summary>
        /// <returns>Position of player</returns>
        public virtual Vector3 GetPlayerPosition()
        {
            return GetPosition();
        }

        /// <summary>
        /// Routine for jumping between sparks
        /// </summary>
        private IEnumerator JumpRoutine()
        {
            if (m_Spark)
            {
                m_IsJumping = true;

                Vector3 from = GetPosition();
                float end = Time.time + m_JumpTime;

                while (Time.time < end)
                {       
                    // Possibility that spark was deactivated while jumping to it
                    if (m_Spark)
                    {
                        float alpha = 1f - Mathf.Clamp01((end - Time.time) / m_JumpTime);
                        Vector3 position = Vector3.Lerp(from, m_Spark.transform.position, alpha);

                        SetPosition(position);

                        m_CachedJumpProgress = alpha;

                        yield return null;
                    }
                    else
                    {
                        // TODO:
                        Debug.LogAssertion("Aborting jump");
                        break;
                    }
                }

                if (m_Spark)
                    m_Spark.AttachJumper(this);

                if (OnJumpToSpark != null)
                    OnJumpToSpark.Invoke(m_Spark, true);

                m_IsJumping = false;
            }
        }

        /// <summary>
        /// Plays audio clip as a selection sound
        /// </summary>
        /// <param name="clip">Clip to play</param>
        public void PlaySelectionSound(AudioClip clip)
        {
            if (m_SelectionAudioSource && clip)
            {
                m_SelectionAudioSource.clip = clip;
                m_SelectionAudioSource.pitch = Random.Range(0.8f, 1.2f);
                m_SelectionAudioSource.Play();
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 100f);
        }
    }
}
