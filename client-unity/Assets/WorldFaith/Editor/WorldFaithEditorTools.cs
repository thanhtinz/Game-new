#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WorldFaith.Client.Audio;
using WorldFaith.Client.VFX;

namespace WorldFaith.Editor
{
    /// <summary>
    /// Editor tools để setup scenes và validate prefabs nhanh.
    /// Menu: WorldFaith > ...
    /// </summary>
    public static class WorldFaithEditorTools
    {
        private const string MenuRoot = "WorldFaith/";

        // ─── Scene Setup ──────────────────────────────────────

        [MenuItem(MenuRoot + "Setup/Create Game Scene Objects")]
        public static void SetupGameScene()
        {
            // Network
            CreateManagerObject<Network.WorldFaithClient>("WorldFaithClient");
            CreateManagerObject<Network.LobbyClient>("LobbyClient");
            CreateManagerObject<Network.ChatClient>("ChatClient");
            CreateManagerObject<Network.MainThreadDispatcher>("MainThreadDispatcher");

            // Managers
            CreateManagerObject<Managers.GameManager>("GameManager");
            CreateManagerObject<Managers.AuthManager>("AuthManager");

            // Audio & VFX
            CreateManagerObject<AudioManager>("AudioManager");
            CreateManagerObject<VfxManager>("VfxManager");
            CreateManagerObject<WorldEventVisualizer>("WorldEventVisualizer");

            // World
            CreateManagerObject<WorldRenderer>("WorldRenderer");
            CreateManagerObject<CameraController>("Main Camera", isCamera: true);

            // UI
            CreateManagerObject<UI.Game.GameHUD>("GameHUD");
            CreateManagerObject<UI.Game.ChatPanel>("ChatPanel");
            CreateManagerObject<UI.Game.EvolutionPanel>("EvolutionPanel");
            CreateManagerObject<UI.Game.ReligionPanel>("ReligionPanel");
            CreateManagerObject<UI.Game.WorldMapUI>("WorldMapUI");
            CreateManagerObject<UI.Game.MiracleCounterUI>("MiracleCounterUI");
            CreateManagerObject<UI.Game.VictoryDefeatScreen>("VictoryDefeatScreen");
            CreateManagerObject<UI.Game.MinimapUI>("MinimapUI");
            CreateManagerObject<UI.Game.SettingsPanel>("SettingsPanel");
            CreateManagerObject<UI.Game.TutorialSystem>("TutorialSystem");
            CreateManagerObject<UI.Game.LeaderboardPanel>("LeaderboardPanel");
            CreateManagerObject<UI.Game.CommandmentPanel>("CommandmentPanel");
            CreateManagerObject<UI.Game.GodSelectionScreen>("GodSelectionScreen");

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("✅ WorldFaith Game Scene objects created (full set)!");
        }

        [MenuItem(MenuRoot + "Setup/Create Lobby Scene Objects")]
        public static void SetupLobbyScene()
        {
            CreateManagerObject<Managers.AuthManager>("AuthManager");
            CreateManagerObject<Network.LobbyClient>("LobbyClient");
            CreateManagerObject<Network.MainThreadDispatcher>("MainThreadDispatcher");
            CreateManagerObject<AudioManager>("AudioManager");
            CreateManagerObject<UI.Lobby.LobbyUI>("LobbyUI");

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("✅ WorldFaith Lobby Scene objects created!");
        }

        [MenuItem(MenuRoot + "Setup/Create Login Scene Objects")]
        public static void SetupLoginScene()
        {
            CreateManagerObject<Managers.AuthManager>("AuthManager");
            CreateManagerObject<Network.MainThreadDispatcher>("MainThreadDispatcher");
            CreateManagerObject<AudioManager>("AudioManager");
            CreateManagerObject<UI.Lobby.LoginUI>("LoginUI");

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("✅ WorldFaith Login Scene objects created!");
        }

        // ─── Validation ───────────────────────────────────────

        [MenuItem(MenuRoot + "Validate/Check AudioManager Setup")]
        public static void ValidateAudioManager()
        {
            var am = Object.FindObjectOfType<AudioManager>();
            if (am == null)
            {
                Debug.LogError("❌ AudioManager không tìm thấy trong scene!");
                return;
            }

            int sfxCount = System.Enum.GetValues(typeof(SfxId)).Length;
            Debug.Log($"AudioManager found. SFX enum count: {sfxCount}");
            Debug.Log("✅ AudioManager setup hợp lệ");
        }

        [MenuItem(MenuRoot + "Validate/Check VfxManager Setup")]
        public static void ValidateVfxManager()
        {
            var vm = Object.FindObjectOfType<VfxManager>();
            if (vm == null)
            {
                Debug.LogError("❌ VfxManager không tìm thấy trong scene!");
                return;
            }

            int vfxCount = System.Enum.GetValues(typeof(VfxId)).Length;
            Debug.Log($"VfxManager found. VFX enum count: {vfxCount}");
            Debug.Log("✅ VfxManager setup hợp lệ");
        }

        [MenuItem(MenuRoot + "Validate/Check All Managers")]
        public static void ValidateAllManagers()
        {
            bool ok = true;

            void Check<T>(string name) where T : MonoBehaviour
            {
                if (Object.FindObjectOfType<T>() == null)
                {
                    Debug.LogWarning($"⚠️ {name} ({typeof(T).Name}) không có trong scene");
                    ok = false;
                }
                else Debug.Log($"✅ {name} ok");
            }

            // Core
            Check<Managers.AuthManager>("AuthManager");
            Check<Managers.GameManager>("GameManager");
            Check<AudioManager>("AudioManager");
            Check<VfxManager>("VfxManager");
            Check<Network.WorldFaithClient>("WorldFaithClient");
            Check<Network.MainThreadDispatcher>("MainThreadDispatcher");
            Check<WorldRenderer>("WorldRenderer");

            // UI
            Check<UI.Game.GameHUD>("GameHUD");
            Check<UI.Game.ChatPanel>("ChatPanel");
            Check<UI.Game.ReligionPanel>("ReligionPanel");
            Check<UI.Game.WorldMapUI>("WorldMapUI");
            Check<UI.Game.MiracleCounterUI>("MiracleCounterUI");
            Check<UI.Game.VictoryDefeatScreen>("VictoryDefeatScreen");
            Check<UI.Game.MinimapUI>("MinimapUI");
            Check<UI.Game.TutorialSystem>("TutorialSystem");
            Check<UI.Game.EvolutionPanel>("EvolutionPanel");

            if (ok) Debug.Log("✅ Tất cả components đầy đủ!");
            else    Debug.LogWarning("⚠️ Một số components còn thiếu — chạy Setup trước");
        }

        // ─── Build Helpers ────────────────────────────────────

        [MenuItem(MenuRoot + "Build/Log Build Info")]
        public static void LogBuildInfo()
        {
            Debug.Log($"Project: {Application.productName} v{Application.version}");
            Debug.Log($"Unity: {Application.unityVersion}");
            Debug.Log($"Platform: {EditorUserBuildSettings.activeBuildTarget}");
            Debug.Log($"Scripting Backend: {PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android)}");
        }

        [MenuItem(MenuRoot + "Build/Set Version")]
        public static void SetVersion()
        {
            string version = EditorUtility.DisplayDialogComplex(
                "Set Version",
                $"Current: {Application.version}",
                "OK", "Cancel", "") == 0
                ? PlayerSettings.bundleVersion
                : null;

            if (version != null)
                Debug.Log($"Version: {Application.version}");
        }

        // ─── Helpers ──────────────────────────────────────────

        private static void CreateManagerObject<T>(string name, bool isCamera = false) where T : Component
        {
            if (Object.FindObjectOfType<T>() != null)
            {
                Debug.Log($"Skipped (already exists): {name}");
                return;
            }

            GameObject go;
            if (isCamera)
            {
                // Check if a Main Camera already exists in the scene
                var existingCam = Camera.main;
                if (existingCam != null)
                    go = existingCam.gameObject;
                else
                {
                    go = new GameObject(name);
                    go.tag = "MainCamera";
                    go.AddComponent<Camera>();
                }

                // Configure for 2D top-down orthographic
                var cam = go.GetComponent<Camera>();
                cam.orthographic     = true;
                cam.orthographicSize = 10f;
                cam.clearFlags       = CameraClearFlags.SolidColor;
                cam.backgroundColor  = new Color(0.05f, 0.05f, 0.1f);
                cam.nearClipPlane    = -100f;
                cam.farClipPlane     = 100f;

                // Position: Z = -10 so it looks toward Z=0 where sprites live
                go.transform.position = new Vector3(32f, 32f, -10f);
                go.transform.rotation = Quaternion.identity;

                // Add AudioListener if missing
                if (go.GetComponent<AudioListener>() == null)
                    go.AddComponent<AudioListener>();
            }
            else
            {
                go = new GameObject(name);
            }

            go.AddComponent<T>();
            Debug.Log($"Created: {name}");
        }
    }

    /// <summary>
    /// Custom inspector cho AudioManager - hiển thị SFX enum mapping.
    /// </summary>
    [CustomEditor(typeof(AudioManager))]
    public class AudioManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("SFX Enum Reference", EditorStyles.boldLabel);

            var sfxIds = System.Enum.GetValues(typeof(SfxId));
            EditorGUI.BeginDisabledGroup(true);
            foreach (SfxId id in sfxIds)
            {
                EditorGUILayout.LabelField($"  [{(int)id}] {id}");
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.HelpBox(
                $"Gán {sfxIds.Length} AudioClips vào sfxClips array theo đúng thứ tự trên.",
                MessageType.Info);
        }
    }
}
#endif
