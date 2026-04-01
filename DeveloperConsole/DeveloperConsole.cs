using Il2Cpp;
using Il2CppAK;
using MelonLoader;
using System.Xml.Linq;
using UnityEngine;
using Il2CppCollection = Il2CppSystem.Collections.Generic;
using Scene = UnityEngine.SceneManagement;

namespace DeveloperConsoleWintermute {

    internal class DeveloperConsole : MelonMod {

        UnityEngine.Object console;

        public override void OnInitializeMelon() 
        {
            UnityEngine.Object console = UnityEngine.Object.Instantiate(Resources.Load("uConsole"));
            uConsole.m_Instance.m_Activate = KeyCode.F1;
            AddConsoleCommands();            
            Debug.Log($"[{Info.Name}] version {Info.Version} loaded!");
        }

        public override void OnApplicationQuit() {
            base.OnApplicationQuit();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
           
        }

        private static void AddConsoleCommands() {
            uConsole.RegisterCommand("scene_name", new Action(() => uConsoleLog.Add(Scene.SceneManager.GetActiveScene().name)));

            uConsole.RegisterCommand("parameter_list", new Action(ListParameters));

            uConsole.RegisterCommand("pos", new Action(GetPosition));

            uConsole.RegisterCommand("tp", new Action(Teleport));
                        
            uConsole.RegisterCommand("get_save_entry", new Action(DeveloperConsole.GetSaveEntry));

            uConsole.RegisterCommand("scene_no_reset", new Action(DeveloperConsole.SceneNoReset));
        }

       

        private static void ListParameters() {
            if(uConsole.GetNumParameters() != 1) {
                uConsoleLog.Add("'parameter_list' takes exactly one parameter.");
                return;
            }

            string commandName = uConsole.GetString();
            List<string> parameters = new List<string>();
            foreach(var parameterSet in uConsoleAutoComplete.m_CommandParameterSets) {
                if (parameterSet.m_Commands.Contains(commandName)) parameters.AddRange(parameterSet.m_AllowedParameters.ToArray());
            }

            if(parameters.Count == 0) {
                uConsoleLog.Add($"'{commandName}' has no registered parameters to display.");
            } else {
                parameters.Sort();
                uConsoleLog.Add("");
                uConsoleLog.Add("Registered Parameters:");
                foreach (string parameter in parameters){
                    uConsoleLog.Add(parameter);
                }
            }
        }

        private static void GetPosition() {
            Vector3 pos = GameManager.GetVpFPSPlayer().transform.position;
            uConsoleLog.Add($"[{pos.x:F2} / {pos.y:F2} / {pos.z:F2}]");
        }

        private static void Teleport() {
            Vector3 target;

            if (uConsole.GetNumParameters() < 2) {
                uConsoleLog.Add("Usage: tp x z    or    tp x y z.\nExample: tp 123 890");
                return;
            } else if (uConsole.GetNumParameters() == 2) {
                float x = uConsole.GetFloat();
                float z = uConsole.GetFloat();

                Vector3 start = new Vector3(x, 10000f, z);
                if (Physics.Raycast(start, Vector3.down, out RaycastHit raycastHit, float.PositiveInfinity, Utils.m_PhysicalCollisionLayerMask | 1048576 | 134217728)) {
                    target = raycastHit.point + new Vector3(0, 0.01f, 0);
                } else {
                    target = new Vector3(x, 0, z);
                }
            } else {
                float x = uConsole.GetFloat();
                float y = uConsole.GetFloat();
                float z = uConsole.GetFloat();
                target = new Vector3(x, y, z);
            }

            Quaternion rot = GameManager.GetVpFPSCamera().transform.rotation;
            GameManager.GetPlayerManagerComponent().TeleportPlayer(target, rot);
            GameManager.GetPlayerManagerComponent().StickPlayerToGround();
        }

       

        private static void GetSaveEntry()
        {
            uConsoleLog.Add(GameManager.m_SceneTransitionData.m_SceneSaveFilenameCurrent);
        }

        private static void SceneNoReset()
        {
            if (uConsole.GetNumParameters() != 1)
            {
                uConsoleLog.Add("'scene_no_reset' takes exactly one parameter.");
                return;
            }
            string @string = uConsole.GetString();
            if (GameManager.m_ActiveScene == @string || GameManager.GetPlayerManagerComponent().PlayerIsDead())
            {
                return;
            }
            if (GameManager.GetPlayerManagerComponent().IsInPlacementMode())
            {
                GameManager.GetPlayerManagerComponent().StowOrDropPlaceMesh();
            }
            GameAudioManager.StopAll();
            uConsole.TurnOff();
            GameManager.m_SceneTransitionData.m_SpawnPointName = null;
            GameManager.m_SceneTransitionData.m_SpawnPointAudio = null;
            //GameManager.m_SceneTransitionData.m_ForceSceneOnNextNavMapLoad = null;
            GameManager.m_SceneTransitionData.m_ForceNextSceneLoadTriggerScene = null;
            //GameManager.m_SceneTransitionData.m_SceneLocationLocIDToShow = null;
            GameManager.m_SceneTransitionData.m_Location = null;
            GameManager.m_SceneTransitionData.m_PosBeforeInteriorLoad = default(Vector3);
            GameManager.m_SceneTransitionData.m_SceneSaveFilenameNextLoad = @string;
            GameManager.LoadScene(@string, GameManager.m_SceneTransitionData.m_SceneSaveFilenameCurrent);
            
            GameManager.GetPlayerManagerComponent().m_SceneTransitionStarted = true;
        }
    }
}
