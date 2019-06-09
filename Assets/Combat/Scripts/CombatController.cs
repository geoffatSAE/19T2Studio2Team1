using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TO5.Combat
{
    /// <summary>
    /// Base controller for the player. Implements shared functionality across multiple platforms
    /// </summary>
    public class CombatController : MonoBehaviour
    {
        /// <summary>
        /// Delegate for the players health changing
        /// </summary>
        /// <param name="damage">The change (+ healed, - damaged)</param>
        /// <param name="health">Remaining health</param>
        public delegate void ControllerHealthChanged(float alpha, float health);

        public ControllerHealthChanged OnHealthChanged;         // Event for when healing or taking damage

        // If the player is dead (out of health)
        public bool isDead { get { return m_Health <= 0f; } }

        public float m_MaxHealth = 100f;        // Players max health
        private float m_Health = 100f;          // Players current health

        [SerializeField] private Text m_HealthText;      // Text for displaying health

        protected virtual void Start()
        {
            m_Health = m_MaxHealth;

            if (m_HealthText)
                m_HealthText.text = string.Format("Health: {0}", m_Health);
        }

        /// <summary>
        /// Damages the player
        /// </summary>
        /// <param name="damage">Damage to apply</param>
        public void TakeDamage(float damage)
        {
            if (!isDead && damage > 0f)
                SetHealth(Mathf.Max(m_Health - damage, 0f));
        }

        /// <summary>
        /// Restores players health
        /// </summary>
        /// <param name="amount">Health to restore</param>
        public void RestoreHealth(float amount)
        {
            if (amount > 0f)
                SetHealth(Mathf.Min(m_Health + amount, m_MaxHealth));
        }

        /// <summary>
        /// Sets players health, calls event
        /// </summary>
        /// <param name="health">New health of player</param>
        private void SetHealth(float health)
        {
            if (health != m_Health)
            {
                // Will be positive if restoring health, negative if dealing damage
                float alpha = health - m_Health;

                if (OnHealthChanged != null)
                    OnHealthChanged.Invoke(alpha, health);

                m_Health = health;

                if (m_HealthText)
                    m_HealthText.text = string.Format("Health: {0}", m_Health);

                // temp
                if (isDead)
                    UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }

        /// <summary>
        /// Fires a single laser from origin at given direction
        /// </summary>
        /// <param name="origin">Origin of shot</param>
        /// <param name="direction">Direction of shot</param>
        protected void Fire(Vector3 origin, Quaternion direction)
        {
            Fire(new Ray(origin, direction * Vector3.forward));
        }

        /// <summary>
        /// Fires a single laser using ray
        /// </summary>
        /// <param name="ray">Ray to cast</param>
        protected void Fire(Ray ray)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, Physics.AllLayers, QueryTriggerInteraction.Collide))
            {
                HandleHit(hit);
            }
        }

        /// <summary>
        /// Handles a confirmed hit after firing
        /// </summary>
        /// <param name="hit">Hit result</param>
        protected virtual void HandleHit(RaycastHit hit)
        {
            GameObject gameObject = hit.collider.gameObject;

            // Handle dealing damage to obstacles
            {
                IObstacle obstacle = gameObject.GetComponent<IObstacle>();
                if (obstacle != null)
                    obstacle.TakeDamage(hit);
            }

            // Handle displaying aesthetics
            {
                PhysicMaterial material = hit.collider.material;             
            }
        }
    }
}
