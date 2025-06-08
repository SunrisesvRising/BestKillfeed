using System;
using Unity.Collections;
using Unity.Entities;
using System.Text;
using Il2CppInterop.Runtime;
using ProjectM;
using BestKillfeed;

public static class QueryComponents
{
    private static Type GetType<T>()
    {
        return typeof(T);
    }

    public static NativeArray<Entity> GetEntitiesByComponentTypes<T1>(EntityQueryOptions queryOption = EntityQueryOptions.Default)
    {
        EntityQueryDesc entityQueryDesc = new EntityQueryDesc()
        {
            All = new ComponentType[] { ComponentType.ReadOnly<T1>() },
            Options = queryOption
        };
        return Core.EntityManager.CreateEntityQuery(entityQueryDesc).ToEntityArray(Allocator.Temp);
    }

}
