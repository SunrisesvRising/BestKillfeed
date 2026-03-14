using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BestKillfeed.Database
{
    public class PlayersData
    {
        [BsonId]
        public string Name { get; set; }
        public ulong SteamID { get; set; }
        public bool HasBounty { get; set; }
        public int MaxLevel { get; set; } = 0;
        public int Kills { get; set; } = 0;
        public int Deaths { get; set; } = 0;
        public int CurrentStreak { get; set; } = 0;
        public int MaxStreak { get; set; } = 0;
    }
}