using System;
using System.Reflection;

namespace BestKillfeed.Utils
{
    public static class FfaBridge
    {
        private static bool _checked = false;
        private static bool _available = false;
        private static MethodInfo _isInFfaZoneMethod = null;

        private static void EnsureChecked()
        {
            if (_checked) return;
            _checked = true;

            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name != "BestFFAZones") continue;

                    var type = asm.GetType("BestFFAZones.Services.ZoneService");
                    if (type == null) break;

                    var method = type.GetMethod(
                        "IsPlayerInFfaZone",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new[] { typeof(ulong) },
                        null
                    );

                    if (method != null)
                    {
                        _isInFfaZoneMethod = method;
                        _available = true;
                        Plugin.Logger.LogInfo("[BestKillfeed] BestFFAZones detected — FFA integration enabled.");
                    }
                    break;
                }

                if (!_available)
                    Plugin.Logger.LogInfo("[BestKillfeed] BestFFAZones not found — FFA integration disabled.");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[BestKillfeed] FfaBridge init failed: {ex.Message}");
            }
        }
        public static bool IsFfaKill(ulong killerSteamId, ulong victimSteamId)
        {
            EnsureChecked();
            if (!_available || _isInFfaZoneMethod == null) return false;

            try
            {
                bool killerInFfa = (bool)_isInFfaZoneMethod.Invoke(null, new object[] { killerSteamId });
                bool victimInFfa = (bool)_isInFfaZoneMethod.Invoke(null, new object[] { victimSteamId });
                return killerInFfa && victimInFfa;
            }
            catch
            {
                return false;
            }
        }
    }
}