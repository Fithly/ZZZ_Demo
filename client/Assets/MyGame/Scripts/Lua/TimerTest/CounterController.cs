using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CounterController : MonoBehaviour
{
    [SerializeField] private CounterView view; // 视图引用
    private CounterModel model;                // 数据模型

    private void Start()
    {
        // 初始化 数据模型
        model = new CounterModel();
        // 初始化 View (将按键与控制器的回调函数绑定（控制器调用数据模型）)
        view.Initialize(OnAddButtonClicked);
        // 将数据变化事件绑定 数据变化就更新View
        model.OnCountChanged += view.UpdateCountDisplay;
        // 初始化显示
        view.UpdateCountDisplay(model.Count);
    }

    private void OnAddButtonClicked()
    {
        // 调用数据模型 修改数据
        model.AddCount(1);
    }
}
