using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

// Gestionnaire unique d'une scène. Il appartient à la scène Game et disparaît avec elle (retour au menu,
// nouvelle partie) : un gestionnaire gardé d'une partie à l'autre pointerait sur une carte détruite.
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _instanceLock = new object();
    private static bool _quitting = false;
    // Image où l'instance a été détruite : pendant le déchargement de la scène, les OnDisable / OnDestroy des
    // autres objets lisent encore Instance, qui ne doit pas recréer un gestionnaire vide.
    private static int _destroyedFrame = -1;

    public static T Instance
    {
        get
        {
            lock (_instanceLock)
            {
                if (_instance == null && !_quitting && Time.frameCount != _destroyedFrame)
                {
                    _instance = GameObject.FindAnyObjectByType<T>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject(typeof(T).ToString());
                        _instance = go.AddComponent<T>();
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
        }
        else if (_instance.GetEntityId() != GetEntityId())
        {
            Destroy(gameObject);
            throw new System.Exception(string.Format("Instance of {0} already exists, removing {1}", GetType().FullName, ToString()));
        }

        Init();
    }

    protected virtual void OnDestroy()
    {
        if (_instance != this) return;
        _instance = null;
        _destroyedFrame = Time.frameCount;
    }

    protected virtual void OnApplicationQuit()
    {
        _quitting = true;
    }

    protected virtual void Init() { }
}
