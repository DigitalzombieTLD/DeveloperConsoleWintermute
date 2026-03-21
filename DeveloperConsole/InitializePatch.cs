using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace DeveloperConsoleWintermute {

    [HarmonyPatch(typeof(BootUpdate), "Start")]
    public static class InitializePatch {

        public static void Prefix() 
        {
            MelonLogger.Msg("uConsole prefix fired");
            UnityEngine.Object console = UnityEngine.Object.Instantiate(Resources.Load("uConsole"));

            if(!console)
            {
                MelonLogger.Msg("console empty :( ");
            }


            uConsole.m_Instance.m_Activate = KeyCode.F1;
        }
    }
}
