using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{ 
    /// <summary>
    /// Controller for the player, can not be instanced by itself.
    /// Provides core functionallity for player interaction
    /// </summary>
    public abstract class SparkJumperAlt : MonoBehaviour
    {
        /// <summary>
        /// Delegate for when jumper is jumping to new spark (both starting and ending)
        /// </summary>
        /// <param name="spark">Spark that has been jumped to (Can be null)</param>
        /// /// <param name="spark">If call is when jump has finished</param>
        public delegate void JumpedToSpark(SparkAlt spark, bool finished);

        public JumpedToSpark OnJumpToSpark;     // Event for when jumping to new spark

        public Transform m_Anchor;                                                  // Anchor to move instead of gameObject
        [SerializeField, Min(0.1f)] private float m_JumpTime = 0.75f;               // Transition time between sparks
        [SerializeField] private LayerMask m_SparkLayer = Physics.AllLayers;        // Layer for sparks

        // If the player is allowed to request a jump
        public bool canJump { get { return !m_IsJumping; } }

        // Spark the player is on
        public SparkAlt spark { get { return m_Spark; } }

        // Wire the player is on
        public WireAlt wire { get { return m_Spark ? m_Spark.GetWire() : null; } }

        private SparkAlt m_Spark;                   // Spark we are on
        private bool m_IsJumping = false;           // If transition is in progress

        /// <summary>
        /// Jumps to given spark
        /// </summary>
        /// <param name="spark">Spark to jump to</param>
        public void JumpToSpark(SparkAlt spark, bool bForce = false)
        {
            if (!spark.canJumpTo && !bForce)
                return;

            if (m_Spark)
                m_Spark.DetachJumper();

            m_Spark = spark;

            if (m_Spark)
            {
                m_Spark.FreezeSwitching();
                StartCoroutine(JumpRoutine());

                if (OnJumpToSpark != null)
                    OnJumpToSpark(m_Spark, false);
            }
        }

        /// <summary>
        /// Traces for a spark in the world
        /// </summary>
        /// <param name="origin">Origin of the trace</param>
        /// <param name="direction">Direction of the trace</param>
        public void TraceSpark(Vector3 origin, Quaternion direction)
        {
            TraceSpark(new Ray(origin, direction * Vector3.forward));
        }

        /// <summary>
        /// Traces for a spark in the world
        /// </summary>
        /// <param name="ray">Ray of the trace</param>
        public void TraceSpark(Ray ray)
        {
            if (canJump)
                return;

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, m_SparkLayer))
            {
                GameObject hitObject = hit.collider.gameObject;

                SparkAlt spark = hitObject.GetComponent<SparkAlt>();
                if (spark)
                    JumpToSpark(spark);      
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
                    // Possibility that spark was deactivated while we were jumping to it
                    if (m_Spark && m_Spark.canJumpTo)
                    {
                        float alpha = Mathf.Clamp01((Time.time - end) / m_JumpTime);
                        Vector3 position = Vector3.Lerp(from, m_Spark.transform.position, alpha);

                        SetPosition(position);

                        yield return null;
                    }
                    else
                    {
                        // TODO:
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

        void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 100f);
        }
    }
}
