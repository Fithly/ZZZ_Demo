using UnityEngine;

/// <summary>
/// 动画事件桥接器：
/// 动画事件只通知 ECS 某个时机到了，不直接扣血
/// </summary>
public class AnimationEventRelay : MonoBehaviour
{
    public Entity entity;

    public void Bind(Entity e)
    {
        entity = e;
    }

    public void OnAttackHit()
    {
        if (entity == null || !entity.Has<AnimationEventComponent>()) return;
        var evt = entity.Get<AnimationEventComponent>();
        evt.HitFrame = true;
        entity.Add(evt);
    }

    public void OnComboWindowOpen()
    {
        if (entity == null || !entity.Has<AnimationEventComponent>()) return;
        var evt = entity.Get<AnimationEventComponent>();
        evt.ComboWindowOpen = true;
        entity.Add(evt);
    }

    public void OnComboWindowClose()
    {
        if (entity == null || !entity.Has<AnimationEventComponent>()) return;
        var evt = entity.Get<AnimationEventComponent>();
        evt.ComboWindowClose = true;
        entity.Add(evt);
    }

    public void OnAttackFinished()
    {
        if (entity == null || !entity.Has<AnimationEventComponent>()) return;
        var evt = entity.Get<AnimationEventComponent>();
        evt.AttackFinished = true;

        entity.Add(evt);
    }

    public void OnRunEndFinished()
    {
        if (entity == null || !entity.Has<AnimationEventComponent>()) return;
        var evt = entity.Get<AnimationEventComponent>();
        evt.RunEndFinished = true;
        entity.Add(evt);
    }
}