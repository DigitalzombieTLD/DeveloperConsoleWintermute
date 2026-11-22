using Il2Cpp;
using Il2CppAK;
using MelonLoader;
using System.Collections;
using System.Reflection;
using System.Xml.Linq;
using UnityEngine;
using Il2CppCollection = Il2CppSystem.Collections.Generic;
using Scene = UnityEngine.SceneManagement;

namespace DeveloperConsoleWintermute {

    internal class DeveloperConsole : MelonMod {

        private const string ConsoleAddress = "uConsole";

        public override void OnInitializeMelon()
        {
            UnityEngine.Object prefab = Resources.Load(ConsoleAddress);
            if (prefab != null)
            {
                UnityEngine.Object.Instantiate(prefab);
                InitializeConsole("Resources");
                return;
            }

            MelonCoroutines.Start(LoadAddressableConsole());
        }

        private IEnumerator LoadAddressableConsole()
        {
            object operation;
            PropertyInfo isDone;
            PropertyInfo result;
            try
            {
                // Resolve Addressables at runtime so the same DLL works in Episodes 1–4.
                Type addressables = Type.GetType("UnityEngine.AddressableAssets.Addressables, Unity.Addressables") ?? throw new TypeLoadException("Unity.Addressables is unavailable.");
                MethodInfo instantiate = addressables.GetMethod("InstantiateAsync", new[]
                {
                    typeof(Il2CppSystem.Object), typeof(Transform), typeof(bool), typeof(bool)
                }) ?? throw new MissingMethodException(addressables.FullName, "InstantiateAsync");

                Type operationType = instantiate.ReturnType;
                isDone = operationType.GetProperty("IsDone") ?? throw new MissingMemberException(operationType.FullName, "IsDone");
                result = operationType.GetProperty("Result") ?? throw new MissingMemberException(operationType.FullName, "Result");
                operation = instantiate.Invoke(null, new object[] { (Il2CppSystem.Object)ConsoleAddress, null, false, true });
            }
            catch (Exception ex)
            {
                LoggerInstance.Warning($"Could not start loading the console from Addressables. {ex.GetBaseException().Message}");
                yield break;
            }

            while (true)
            {
                try
                {
                    if (isDone.GetValue(operation) is true)
                    {
                        if (result.GetValue(operation) is GameObject instance && instance != null)
                            InitializeConsole("Addressables");
                        else
                            LoggerInstance.Warning($"Addressables did not load the '{ConsoleAddress}' prefab.");

                        yield break;
                    }
                }
                catch (Exception ex)
                {
                    LoggerInstance.Warning($"Could not load the console from Addressables. {ex.GetBaseException().Message}");
                    yield break;
                }

                yield return null;
            }
        }

        private void InitializeConsole(string source)
        {
            uConsole console = uConsole.m_Instance;
            if (console == null)
            {
                LoggerInstance.Warning($"The console prefab loaded via {source} without initializing uConsole.");
                return;
            }

            console.m_Activate = KeyCode.F1;
            AddConsoleCommands();
            LoggerInstance.Msg($"Console loaded via {source}. Press F1 to open it.");
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
