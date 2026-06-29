using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldFaith.Client.Network
{
    /// <summary>
    /// Dispatch actions từ background thread (SignalR) sang Unity main thread.
    /// Attach vào GameObject cùng scene với WorldFaithClient.
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static readonly Queue<Action> Queue = new();
        private static readonly object Lock = new();
        private static MainThreadDispatcher _instance;

        private void Awake()
        {
            if (_instance != null) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void Enqueue(Action action)
        {
            lock (Lock)
            {
                Queue.Enqueue(action);
            }
        }

        private void Update()
        {
            lock (Lock)
            {
                while (Queue.Count > 0)
                {
                    try
                    {
                        Queue.Dequeue().Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[MainThreadDispatcher] Lỗi: {ex}");
                    }
                }
            }
        }
    }
}
