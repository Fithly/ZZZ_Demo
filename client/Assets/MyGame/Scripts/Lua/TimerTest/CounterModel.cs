using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CounterModel : MonoBehaviour
{
    private int count = 0; // 计数数据
    public int Count
    {
        get { return count; }
    }

    public event Action<int> OnCountChanged; // 数值变化事件

    // 修改计数数据
    public void AddCount(int value)
    {
        count += value;
        // 触发事件 通知View更改视图
        OnCountChanged?.Invoke(count);
    }
}
