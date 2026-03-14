using LiteDB;
using System.IO;
using BepInEx;
using BestKillfeed.Database;
using UnityEngine;
namespace BestKillfeed.Services
{
    public class DatabaseService
    {
        private static readonly string DbPath = Path.Combine(Paths.ConfigPath, "BestKillfeed", "KillfeedData.db");
        public static LiteDatabase DB { get; private set; }
        public static void Initialize()
        {
            if (DB != null) return;
            try
            {
                var directory = Path.GetDirectoryName(DbPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                DB = new LiteDatabase(new ConnectionString(DbPath)
                {
                    Connection = ConnectionType.Direct
                });
                var players = DB.GetCollection<PlayersData>("players");
                players.EnsureIndex(x => x.Name, true);
                Debug.Log("[BestKillfeed] LiteDB database operational.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BestKillfeed] LiteDB error: {ex.Message}");
            }
        }
        public static void Shutdown()
        {
            if (DB != null)
            {
                DB.Dispose();
                DB = null;
            }
        }
    }
}