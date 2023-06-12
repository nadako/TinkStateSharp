using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]

namespace TinkState
{
    public class UnityBatchScheduler : Scheduler
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Observable.Scheduler = Instance;
            PlayerLoopHelper.ModifyPlayerLoop(typeof(PostLateUpdate), typeof(UnityBatchScheduler), OnUpdate);
        }

        static void OnUpdate()
        {
            var scheduler = Instance;
            if (scheduler.scheduled)
            {
                scheduler.Progress(0.1f);
            }
        }

        static readonly UnityBatchScheduler Instance = new UnityBatchScheduler();

        List<Schedulable> queue = new List<Schedulable>();
        List<Schedulable> nextQueue = new List<Schedulable>();
        bool scheduled;

        UnityBatchScheduler() { }

        public void Schedule(Schedulable schedulable)
        {
            queue.Add(schedulable);
            scheduled = true;
        }

        void Progress(float maxSeconds)
        {
            var end = GetTimeStamp() + maxSeconds;
            do
            {
                // to handle the unfortunate case where a binding invocation schedules another one
                // we have two queues and swap between them to avoid allocating a new list every time
                var currentQueue = queue;
                queue = nextQueue;
                foreach (var o in currentQueue) o.Run();
                currentQueue.Clear();
                nextQueue = currentQueue;
            }
            while (queue.Count > 0 && GetTimeStamp() < end);

            if (queue.Count == 0)
            {
                scheduled = false;
            }
        }

        float GetTimeStamp()
        {
            return Time.realtimeSinceStartup;
        }
    }

    static class PlayerLoopHelper
    {
        public static void ModifyPlayerLoop(Type where, Type what, PlayerLoopSystem.UpdateFunction updateFunction)
        {
            var newSystem = new PlayerLoopSystem
            {
                type = what,
                updateDelegate = updateFunction
            };
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            var index = FindLoopSystemIndex(playerLoop.subSystemList, where);
            var postLateUpdateSystem = playerLoop.subSystemList[index];
            postLateUpdateSystem.subSystemList = AppendLoopSystem(postLateUpdateSystem.subSystemList, newSystem);
            playerLoop.subSystemList[index] = postLateUpdateSystem;
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        static PlayerLoopSystem[] AppendLoopSystem(PlayerLoopSystem[] subSystemList, PlayerLoopSystem system)
        {
            var newSubSystemList = new PlayerLoopSystem[subSystemList.Length + 1];
            Array.Copy(subSystemList, 0, newSubSystemList, 0, subSystemList.Length);
            newSubSystemList[subSystemList.Length] = system;
            return newSubSystemList;
        }

        static int FindLoopSystemIndex(PlayerLoopSystem[] subSystemList, Type systemType)
        {
            for (int i = 0; i < subSystemList.Length; i++)
            {
                if (subSystemList[i].type == systemType)
                {
                    return i;
                }
            }
            throw new Exception("Target PlayerLoopSystem does not found. Type:" + systemType.FullName);
        }
    }
}
