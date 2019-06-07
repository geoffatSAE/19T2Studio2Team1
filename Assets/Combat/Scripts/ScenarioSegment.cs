using System;
using UnityEngine;

namespace TO5.Combat
{
    public class ScenarioSegment
    {
        [Serializable]
        public struct TargetPreset
        {
            public GameObject m_TargetPrefab;
            public int m_Amount;
            public int m_InstanceLimit;

            private int m_Count;
        }

        [SerializeField] protected TargetPreset[] m_TargetPresets;

        public virtual void Start()
        {

        }

        public virtual void Update()
        {

        }

        public virtual void Cleanup()
        {

        }
    }
}