using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace WankulCrazyPlugin.utils
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new();

        public static void SetTestInstance(T instance)
        {
            _instance = instance;
        }

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        try
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
                        catch (Exception)
                        {
                            // Hors-runtime Unity (ex: tests unitaires xUnit), instancier via GetUninitializedObject sans appeler les C++ internal calls de MonoBehaviour
                            _instance = (T)FormatterServices.GetUninitializedObject(typeof(T));
                        }
                    }
                    return _instance;
                }
            }
        }

        protected Singleton() { }
    }
}
