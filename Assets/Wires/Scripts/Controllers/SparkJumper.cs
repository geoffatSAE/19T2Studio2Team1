using System.Collections;
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

        public JumpedToSpark OnJumpToSpark;     // Event for when jumping to new spark

        public Transform m_Anchor;                                                          // Anchor to move instead of gameObject
        [SerializeField, Min(0.1f)] private float m_JumpTime = 0.75f;                       // Transition time between sparks
        [SerializeField, Min(0.1f)] protected float m_TraceRadius = 0.5f;                   // Radius of sphere cast
        [SerializeField] private LayerMask m_InteractiveLayer = Physics.AllLayers;          // Layer for interactives

        // If the player is allowed to request a jump
        public bool canJump { get { return !m_IsJumping; } }

        // Spark the player is on
        public Spark spark { get { return m_Spark; } }

        // Wire the player is on
        public Wire wire { get { return m_Spark ? m_Spark.GetWire() : null; } }

        // If player is jumping
        public bool isJumping { get { return m_IsJumping; } }

        // Progress of jump
        public float jumpProgress { get { return m_IsJumping ? m_CachedJumpProgress : 0f; } }

        // Distance travelled on wires
        public float wireDistanceTravelled { get { return m_WireDistanceTravelled; } set { m_WireDistanceTravelled = value; } }

        private Spark m_Spark;                          // Spark we are on
        private bool m_IsJumping = false;               // If transition is in progress
        private float m_CachedJumpProgress = -1f;       // Cached progress of jump
        private float m_WireDistanceTravelled = 0f;     // Distance jumper has travelled while on wires

        /// <summary>
        /// Jumps to given spark
        /// </summary>
        /// <param name="spark">Spark to jump to</param>
        public void JumpToSpark(Spark spark, bool bForce = false)
        {
            if (!spark || !spark.canJumpTo && !bForce)
                return;

            if (m_Spark)
                m_Spark.DetachJumper();

            m_Spark = spark;

            m_Spark.FreezeSwitching();
            m_Spark.AttachJumper(this);

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

            m_Spark.FreezeSwitching();
            m_Spark.AttachJumper(this);
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
            if (!canJump)
                return;

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
                        Debug.Log("Aborting jump");
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

    public static class ExtDebug
    {
        //Draws just the box at where it is currently hitting.
        public static void DrawBoxCastOnHit(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float hitInfoDistance, Color color)
        {
            origin = CastCenterOnCollision(origin, direction, hitInfoDistance);
            DrawBox(origin, halfExtents, orientation, color);
        }

        //Draws the full box from start of cast to its end distance. Can also pass in hitInfoDistance instead of full distance
        public static void DrawBoxCastBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float distance, Color color)
        {
            direction.Normalize();
            Box bottomBox = new Box(origin, halfExtents, orientation);
            Box topBox = new Box(origin + (direction * distance), halfExtents, orientation);

            Debug.DrawLine(bottomBox.backBottomLeft, topBox.backBottomLeft, color);
            Debug.DrawLine(bottomBox.backBottomRight, topBox.backBottomRight, color);
            Debug.DrawLine(bottomBox.backTopLeft, topBox.backTopLeft, color);
            Debug.DrawLine(bottomBox.backTopRight, topBox.backTopRight, color);
            Debug.DrawLine(bottomBox.frontTopLeft, topBox.frontTopLeft, color);
            Debug.DrawLine(bottomBox.frontTopRight, topBox.frontTopRight, color);
            Debug.DrawLine(bottomBox.frontBottomLeft, topBox.frontBottomLeft, color);
            Debug.DrawLine(bottomBox.frontBottomRight, topBox.frontBottomRight, color);

            DrawBox(bottomBox, color);
            DrawBox(topBox, color);
        }

        public static void DrawBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Color color)
        {
            DrawBox(new Box(origin, halfExtents, orientation), color);
        }
        public static void DrawBox(Box box, Color color)
        {
            Debug.DrawLine(box.frontTopLeft, box.frontTopRight, color);
            Debug.DrawLine(box.frontTopRight, box.frontBottomRight, color);
            Debug.DrawLine(box.frontBottomRight, box.frontBottomLeft, color);
            Debug.DrawLine(box.frontBottomLeft, box.frontTopLeft, color);

            Debug.DrawLine(box.backTopLeft, box.backTopRight, color);
            Debug.DrawLine(box.backTopRight, box.backBottomRight, color);
            Debug.DrawLine(box.backBottomRight, box.backBottomLeft, color);
            Debug.DrawLine(box.backBottomLeft, box.backTopLeft, color);

            Debug.DrawLine(box.frontTopLeft, box.backTopLeft, color);
            Debug.DrawLine(box.frontTopRight, box.backTopRight, color);
            Debug.DrawLine(box.frontBottomRight, box.backBottomRight, color);
            Debug.DrawLine(box.frontBottomLeft, box.backBottomLeft, color);
        }

        public struct Box
        {
            public Vector3 localFrontTopLeft { get; private set; }
            public Vector3 localFrontTopRight { get; private set; }
            public Vector3 localFrontBottomLeft { get; private set; }
            public Vector3 localFrontBottomRight { get; private set; }
            public Vector3 localBackTopLeft { get { return -localFrontBottomRight; } }
            public Vector3 localBackTopRight { get { return -localFrontBottomLeft; } }
            public Vector3 localBackBottomLeft { get { return -localFrontTopRight; } }
            public Vector3 localBackBottomRight { get { return -localFrontTopLeft; } }

            public Vector3 frontTopLeft { get { return localFrontTopLeft + origin; } }
            public Vector3 frontTopRight { get { return localFrontTopRight + origin; } }
            public Vector3 frontBottomLeft { get { return localFrontBottomLeft + origin; } }
            public Vector3 frontBottomRight { get { return localFrontBottomRight + origin; } }
            public Vector3 backTopLeft { get { return localBackTopLeft + origin; } }
            public Vector3 backTopRight { get { return localBackTopRight + origin; } }
            public Vector3 backBottomLeft { get { return localBackBottomLeft + origin; } }
            public Vector3 backBottomRight { get { return localBackBottomRight + origin; } }

            public Vector3 origin { get; private set; }

            public Box(Vector3 origin, Vector3 halfExtents, Quaternion orientation) : this(origin, halfExtents)
            {
                Rotate(orientation);
            }
            public Box(Vector3 origin, Vector3 halfExtents)
            {
                this.localFrontTopLeft = new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
                this.localFrontTopRight = new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
                this.localFrontBottomLeft = new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
                this.localFrontBottomRight = new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);

                this.origin = origin;
            }


            public void Rotate(Quaternion orientation)
            {
                localFrontTopLeft = RotatePointAroundPivot(localFrontTopLeft, Vector3.zero, orientation);
                localFrontTopRight = RotatePointAroundPivot(localFrontTopRight, Vector3.zero, orientation);
                localFrontBottomLeft = RotatePointAroundPivot(localFrontBottomLeft, Vector3.zero, orientation);
                localFrontBottomRight = RotatePointAroundPivot(localFrontBottomRight, Vector3.zero, orientation);
            }
        }

        //This should work for all cast types
        static Vector3 CastCenterOnCollision(Vector3 origin, Vector3 direction, float hitInfoDistance)
        {
            return origin + (direction.normalized * hitInfoDistance);
        }

        static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
        {
            Vector3 direction = point - pivot;
            return pivot + rotation * direction;
        }
    }
}
