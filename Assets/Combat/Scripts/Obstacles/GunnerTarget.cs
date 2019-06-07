using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Combat
{
    public class GunnerTarget : Target
    {
        public float m_Accuracy = 0.8f;         // Accuracy of the gunner, 0 means complete accuracy
        public float m_AccuracyOffset = 1f;     // Offset of inaccurate shots, so we don't wildly shoot everywhere


    }
}
