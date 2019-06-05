using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    public class Wire : MonoBehaviour
    { 
        public Spark spark { get; private set; }

        public float m_Distance = 15f;         // The distance of this wire

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

            spark.transform.position += WireManager.WirePlane * spark.m_Speed * Time.deltaTime;

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

        public void SpawnSpark(Spark prefab)
        {
            spark = Instantiate(prefab, transform.position, Quaternion.identity);
            spark.InitializerSpark(this);
        }

        public void InitializeWire(float distance, Spark prefab, float sparkDelay = 0f)
        {
            m_Distance = distance;

            if (sparkDelay <= 0f)
                SpawnSpark(prefab);
        }

        public Vector3 GetEnd()
        {
            return transform.position + (WireManager.WirePlane * m_Distance);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.15f);
            Gizmos.DrawLine(transform.position, transform.position + (WireManager.WirePlane * m_Distance));
        }
    }
}
