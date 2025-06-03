using System.Collections.Generic;
using Unity.Entities;

namespace BestKillfeed.Utils
{
    public static class KillCache
    {
        private static readonly Dictionary<Entity, Entity> DownedBy = new();

        public static void SetDowner(Entity victim, Entity killer)
        {
            DownedBy[victim] = killer;
        }

        public static Entity? GetDowner(Entity victim)
        {
            return DownedBy.TryGetValue(victim, out var killer) ? killer : (Entity?)null;
        }

        public static void Clear(Entity victim)
        {
            DownedBy.Remove(victim);
        }
    }
}
