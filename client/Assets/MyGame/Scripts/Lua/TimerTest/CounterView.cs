using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CounterView : MonoBehaviour
{
    [SerializeField] private TextMeshPro countText;
    [SerializeField] private Button addButton;

    // 初始化
    public void Initialize(Action onClickAddButton)
    {
        // 将鼠标点击事件 和 控制器的回调函数进行绑定
        addButton.onClick.AddListener(() => {
            onClickAddButton?.Invoke();
        });
    }

    // 更新文本显示
    public void UpdateCountDisplay(int count)
    {
        countText.text = "Count：" + count;
    }

}
