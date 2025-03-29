using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;

namespace DataKeeper.Helpers
{
    public static class PlayerLoopHelper
    {
        public static void RemoveSystemFromPlayerLoop(Type targetSystemType, Type systemToRemove)
        {
            PlayerLoopSystem playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            RemoveSystemFromPlayerLoop(ref playerLoop, targetSystemType, systemToRemove);
            PlayerLoop.SetPlayerLoop(playerLoop);
        }
        public static void InsertSystemIntoPlayerLoop(Type targetSystemType, PlayerLoopSystem newSystem)
        {
            PlayerLoopSystem playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            InsertSystemIntoPlayerLoop(ref playerLoop, targetSystemType, newSystem);
            PlayerLoop.SetPlayerLoop(playerLoop);
        }
        private static void RemoveSystemFromPlayerLoop(ref PlayerLoopSystem playerLoop, Type targetLoopType, Type systemToRemove)
        {
            if (playerLoop.type == targetLoopType && playerLoop.subSystemList != null)
            {
                var subsystemsList = new List<PlayerLoopSystem>(playerLoop.subSystemList);
                subsystemsList.RemoveAll(system => system.type == systemToRemove);
                playerLoop.subSystemList = subsystemsList.ToArray();
            }

            if (playerLoop.subSystemList != null)
            {
                for (int i = 0; i < playerLoop.subSystemList.Length; i++)
                {
                    if (playerLoop.subSystemList[i].type == targetLoopType)
                    {
                        var subsystem = playerLoop.subSystemList[i];
                        RemoveSystemFromPlayerLoop(ref subsystem, targetLoopType, systemToRemove);
                        playerLoop.subSystemList[i] = subsystem;
                    }
                }
            }
        }
        private static void InsertSystemIntoPlayerLoop(ref PlayerLoopSystem playerLoop, Type targetSystemType, PlayerLoopSystem newSystem)
        {
            if (playerLoop.type == targetSystemType)
            {
                PlayerLoopSystem[] newSubSystems;
                if (playerLoop.subSystemList != null)
                {
                    newSubSystems = new PlayerLoopSystem[playerLoop.subSystemList.Length + 1];
                    Array.Copy(playerLoop.subSystemList, newSubSystems, playerLoop.subSystemList.Length);
                    newSubSystems[newSubSystems.Length - 1] = newSystem;
                }
                else
                {
                    newSubSystems = new PlayerLoopSystem[] { newSystem };
                }
                playerLoop.subSystemList = newSubSystems;
                return;
            }

            if (playerLoop.subSystemList != null)
            {
                for (int i = 0; i < playerLoop.subSystemList.Length; i++)
                {
                    InsertSystemIntoPlayerLoop(ref playerLoop.subSystemList[i], targetSystemType, newSystem);
                }
            }
        }

        public static void DebugLogAllPlayerLoop()
        {
            PlayerLoopSystem playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            Debug.Log("===== PLAYER LOOP SYSTEMS =====");
            PrintPlayerLoopSystem(playerLoop, 0);
            Debug.Log("==============================");
        }

        private static void PrintPlayerLoopSystem(PlayerLoopSystem system, int indentLevel)
        {
            string indent = new string(' ', indentLevel * 2);

            // Log the current system
            if (system.type != null)
            {
                Debug.Log($"{indent}+ {system.type.Name}");
            }
            else
            {
                Debug.Log($"{indent}+ [Root]");
            }

            // Recursively log all subsystems
            if (system.subSystemList != null)
            {
                foreach (PlayerLoopSystem subSystem in system.subSystemList)
                {
                    PrintPlayerLoopSystem(subSystem, indentLevel + 1);
                }
            }
        }
    }
}
