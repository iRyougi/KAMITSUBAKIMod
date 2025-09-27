using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KAMITSUBAKIMod.Runtime;
using UnityEngine;

namespace KAMITSUBAKIMod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("kamitsubaki.framework", BepInDependency.DependencyFlags.HardDependency)] // ȷ��������ڱ� Mod ����
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

            // �����������
            var go = new GameObject("KAMITSUBAKIModRunner") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go); go.AddComponent<UpdateLogger>();

            var scannerGO = new GameObject("KAMITSUBAKI_BookScanner") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(scannerGO); scannerGO.AddComponent<BookScanner>();

            var rewriterGO = new GameObject("KAMITSUBAKI_BookLiveRewriter") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(rewriterGO); rewriterGO.AddComponent<BookLiveRewriter>();

            var editorGO = new GameObject("KAMITSUBAKI_StoryEditorGUI") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(editorGO); editorGO.AddComponent<StoryEditorGUI>();

            // Harmony ֻ��ʼ��һ��
            _harmony = new Harmony(PluginGuid);
            try
            {
                _harmony.PatchAll();
                Log.LogInfo($"{PluginName} {PluginVersion} patched");
            }
            catch (System.Exception e)
            {
                Log.LogError("Harmony PatchAll failed: " + e);
            }

            // �����ı��滻ӳ��
            Text.TextBookMap.Load();

            // ��ϣ�ȷ�Ͽ���Ƿ��Ѽ���
            if (KAMITSUBAKI.Framework.FrameworkPlugin.Assets == null)
                Log.LogWarning("FrameworkPlugin.Assets is null (framework may not have loaded!)");
            else
                Log.LogInfo("Framework detected (Assets service ready)");

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void OnDestroy()
        {
            try { _harmony?.UnpatchSelf(); } catch { }
        }
    }

    public class UpdateLogger : MonoBehaviour
    {
        float _timer;
        void Update()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer >= 5f)
            {
                _timer = 0f;
                // ���ڴ��������������־��ȷ��δ����
                // Plugin.Log?.LogDebug("[Heartbeat]");
            }
        }
    }
}
