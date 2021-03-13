using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.LEGO.Behaviours
{
    public class LEGOBehaviourCoroutineManager : MonoBehaviour
    {
        static LEGOBehaviourCoroutineManager m_Instance;
        static readonly Dictionary<Object, Coroutine> s_ExistingCoroutines = new Dictionary<Object, Coroutine>();

        public static void StartCoroutine(Object owner, IEnumerator coroutine, bool stopExisting = false)
        {
            if (m_Instance)
            {
                if (stopExisting && s_ExistingCoroutines.ContainsKey(owner))
                {
                    m_Instance.StopCoroutine(s_ExistingCoroutines[owner]);
                }

                s_ExistingCoroutines.Remove(owner);
                s_ExistingCoroutines.Add(owner, m_Instance.StartCoroutine(coroutine));
            }
        }

        void Awake()
        {
            if (m_Instance && m_Instance != this)
            {
                Destroy(this);
            }
            else
            {
                m_Instance = this;
            }
        }
    }
}
