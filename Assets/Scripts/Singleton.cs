using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _instanceLock = new object();
    private static bool _quitting = false;

    public static T Instance
    {
        get
        {
            lock (_instanceLock)
            {
                if (_instance == null && !_quitting)
                {
                    _instance = GameObject.FindAnyObjectByType<T>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject(typeof(T).ToString());
                        _instance = go.AddComponent<T>();
                        if (_instance.transform.parent == null) DontDestroyOnLoad(_instance.gameObject);
                    }
                }

                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null) {
            _instance = gameObject.GetComponent<T>();
            // DontDestroyOnLoad ne fonctionne que sur les objets à la racine
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }
        else if (_instance.GetEntityId() != GetEntityId())
        {
            Destroy(gameObject);
            throw new System.Exception(string.Format("Instance of {0} already exists, removing {1}", GetType().FullName, ToString()));
        }
        
        Init();
    }

    protected virtual void OnApplicationQuit()
    {
        _quitting = true;
    }

    protected virtual void Init() { }
}