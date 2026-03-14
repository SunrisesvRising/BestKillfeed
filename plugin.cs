using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using VampireCommandFramework;
using System.IO;
using BepInEx.Logging;
using BestKillfeed.Services;
namespace BestKillfeed
{
    [BepInPlugin("com.tonpseudo.bestkillfeed", "BestKillfeed", "1.0.0")]
    public class Plugin : BasePlugin
    {
        private Harmony _harmony;
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Logger;
        public override void Load()
        {
            Instance = this;
            Logger = Log;
            var configPath = Path.Combine(BepInEx.Paths.ConfigPath, "Bestkillfeed");
            if (!Directory.Exists(configPath))
                Directory.CreateDirectory(configPath);
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} version {MyPluginInfo.PLUGIN_VERSION} is loading...");
            try
            {
                DatabaseService.Initialize();
                Log.LogInfo("LiteDB initialized successfully.");
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Fatal error during database initialization: {ex.Message}");
            }
            KillfeedSettings.Init(configPath);
            CommandRegistry.RegisterAll();
            _harmony = new Harmony("com.tonpseudo.bestkillfeed");
            _harmony.PatchAll();
            Log.LogInfo("BestKillfeed loaded and ready!");
        }
        public override bool Unload()
        {
            DatabaseService.Shutdown();
            _harmony?.UnpatchSelf();
            return base.Unload();
        }
    }
}