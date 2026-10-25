using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ECS 实体和 Unity GameObject 的绑定组件
/// 用于 DestroySystem 统一销毁 GameObject
/// </summary>
public struct ViewComponent
{
    public GameObject Owner;
    public bool DestroyWithEntity;
}