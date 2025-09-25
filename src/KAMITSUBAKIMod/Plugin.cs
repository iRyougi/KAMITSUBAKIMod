using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace KAMITSUBAKIMod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.iryougi.kcr.kamitsubakimod";
        public const string PluginName = "KAMITSUBAKIMod";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            // 1) Start a simple runner that logs in Update (visible proof the mod is running)
            var go = new GameObject("KAMITSUBAKIModRunner");
            GameObject.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<UpdateLogger>();

            // 2) (Optional) Apply Harmony patches declared with [HarmonyPatch]
            _harmony = new Harmony(PluginGuid);
            try
            {
                _harmony.PatchAll();
                Log.LogInfo($"{PluginName} {PluginVersion} loaded (PatchAll ok)");
            }
            catch (System.Exception e)
            {
                Log.LogWarning($"Harmony PatchAll warning: {e.Message}");
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

    // A tiny MonoBehaviour to demonstrate Update() logging without touching game code
    public class UpdateLogger : MonoBehaviour
    {
        private float _timer;
        void Update()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer >= 2f) // log every ~2 seconds to avoid spam
            {
                _timer = 0f;
                Plugin.Log?.LogInfo("[KAMITSUBAKIMod] Update tick");
            }
        }
    }
}
