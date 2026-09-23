using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace WankulCrazyPlugin.utils
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static T _instance;
        private static readonly object _lock = new();

        public static void SetManualInstance(T instance)
        {
            _instance = instance;
        }

        public static T Instance
        {
            get
            {
                if (_instance != null) return _instance;
                try
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = FindObjectOfType<T>();

                            if (_instance == null)
                            {
                                GameObject singletonObject = new GameObject();
                                _instance = singletonObject.AddComponent<T>();
                                singletonObject.name = typeof(T).ToString() + " (Singleton)";
                                DontDestroyOnLoad(singletonObject);
                            }
                        }
                        return _instance;
                    }
                }
                catch (Exception)
                {
                    if (_instance == null)
                    {
                        _instance = (T)FormatterServices.GetUninitializedObject(typeof(T));
                    }
                    return _instance;
                }
            }
        }

        protected Singleton() { }
    }
}
