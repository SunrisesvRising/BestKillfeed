using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using VampireCommandFramework;
using System.IO;
using BepInEx.Logging;

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
            // S'assurer que le dossier existe
            var configPath = Path.Combine(BepInEx.Paths.ConfigPath, "Bestkillfeed");
            if (!Directory.Exists(configPath))
                Directory.CreateDirectory(configPath);
            Instance = this;
            Logger = Log;
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} version {MyPluginInfo.PLUGIN_VERSION} is loading...");
            Log.LogInfo("BestKillfeed chargé.");

            // Enregistre toutes les commandes
            CommandRegistry.RegisterAll();

            // Charge les Settings
            KillfeedSettings.Init(configPath);

            // Initialise Harmony et applique les patches
            _harmony = new Harmony("com.tonpseudo.bestkillfeed");
            _harmony.PatchAll();
        }
    }
}
