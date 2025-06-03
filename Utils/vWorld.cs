using Unity.Entities;

namespace BestKillfeed.Utils;

public static class VWorld
{
    public static World Server
    {
        get
        {
            foreach (var world in World.All)
            {
                if (world.Name == "Server")
                    return world;
            }
            return null;
        }
    }

    public static EntityManager ServerEntityManager => Server.EntityManager;
}