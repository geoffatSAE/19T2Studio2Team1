using System;
using System.Collections;
using UnityEngine;

namespace TO5.Wires.Legacy
{
    [Obsolete]
    public class WireLegacy : MonoBehaviour
    { 
        public SparkLegacy spark { get; private set; }

        public float m_Distance = 15f;         // The distance of this wire
        public Renderer m_WireMesh;
        public Transform m_Pivot;

        void OnDestroy()
        {
            if (spark)
                Destroy(spark.gameObject);
        }

        /// <summary>
        /// Ticks the spark, having it travel along the wire
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <returns></returns>
        public float TickSpark(float deltaTime)
        {
            if (!spark)
                return 0f;

            spark.transform.position += WireManagerLegacy.WirePlane * spark.m_Speed * Time.deltaTime;

            Vector3 offset = spark.transform.position - transform.position;
            float distance = offset.sqrMagnitude;

            if (spark.Jumper)
            {
                Vector3 jumperPosition = spark.Jumper.transform.position;
                jumperPosition.z = spark.transform.position.z;
                spark.Jumper.transform.position = jumperPosition;
            }

            float alpha = distance / (GetEnd() - transform.position).sqrMagnitude;
            return alpha;
        }

        public void SpawnSpark(SparkLegacy prefab)
        {
            spark = Instantiate(prefab, transform.position, Quaternion.identity);
            spark.InitializerSpark(this);
        }

        public void InitializeWire(float distance, SparkLegacy prefab, float sparkDelay = 0f)
        {
            m_Distance = distance;

            if (sparkDelay <= 0f)
                SpawnSpark(prefab);
            else
                StartCoroutine(DelaySpawnSpark(sparkDelay, prefab));
        }

        private IEnumerator DelaySpawnSpark(float delay, SparkLegacy prefab)
        {
            yield return new WaitForSeconds(delay);
            SpawnSpark(prefab);
        }

        public Vector3 GetEnd()
        {
            return transform.position + (WireManagerLegacy.WirePlane * m_Distance);
        }

        public void SetPositionAndDistance(Vector3 position, float distance)
        {
            transform.position = position;
            m_Distance = distance;

            if (m_WireMesh && m_Pivot)
            {
                // Bounds is in world space
                Bounds meshBounds = m_WireMesh.bounds;
                float scaler = distance / meshBounds.size.z;

                Vector3 scale = m_Pivot.transform.localScale;
                scale.y = scaler;
                m_Pivot.transform.localScale = scale;
            }
        }
    }
}
