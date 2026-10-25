using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroySystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        List<int> removeIds = new List<int>();
        List<GameObject> destroyViews = new List<GameObject>();

        foreach (var entity in world.entities)
        {
            if (!entity.Has<PendingDestroyComponent>())
                continue;

            // 放入需要消除的列表中
            var pending = entity.Get<PendingDestroyComponent>();
            if (!pending.NeedDestroy)
                continue;

            // 如果有ViewComponent，则先删除Gameobject再Remove Entity
            if (entity.TryGet<ViewComponent>(out var viewComp))
            {
                // 房主正式销毁怪物前，广播 BattleDestroy
                // 注意：这里代表死亡动画倒计时已经结束，是真正销毁节点
                if (NetworkBattleEntry.Instance != null &&
                    NetworkBattleEntry.Instance.IsHost &&
                    entity.Has<MonsterNetworkIdentityComponent>())
                {
                    var monsterNet = entity.Get<MonsterNetworkIdentityComponent>();

                    if (monsterNet.IsHostControlled)
                    {
                        // 销毁怪物（2）
                        NetworkBattleEntry.Instance.SendBattleDestroy(monsterNet.MonsterId, 2);
                    }
                }

                if (viewComp.DestroyWithEntity && viewComp.Owner != null)
                {
                    destroyViews.Add(viewComp.Owner);
                }
            }

            removeIds.Add(entity.ID);
        }

        foreach (var go in destroyViews)
        {
            if(go != null)
                Object.Destroy(go);
        }

        // 调用world来进行实体移除
        foreach (var id in removeIds)
        {
            world.RemoveEntity(id);
        }
    }
}
