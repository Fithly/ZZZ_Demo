using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 网络调试面板
/// 
/// 职责：
/// 1. 显示当前网络状态
/// 2. 方便演示网络同步系统
/// 3. 方便定位状态包 / 快照插值问题
/// 
/// 快捷键：
/// F3 显示 / 隐藏
/// </summary>
public class NetworkDebugOverlay : MonoBehaviour
{
    [SerializeField] private bool visible = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F3;

    private GUIStyle labelStyle;
    private Rect windowRect = new Rect(10f, 10f, 300f, 400f);

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            visible = !visible;
        }
    }

    private void OnGUI()
    {
        if (!visible)
            return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 15;
            labelStyle.normal.textColor = Color.white;
        }

        windowRect = GUI.Window(1001, windowRect, DrawWindow, "Network Debug");
    }

    private void DrawWindow(int id)
    {
        NetworkBattleEntry entry = NetworkBattleEntry.Instance;

        if (entry == null)
        {
            GUILayout.Label("NetworkBattleEntry: null", labelStyle);
            GUI.DragWindow();
            return;
        }

        GUILayout.Label("LocalPlayerId: " + entry.LocalPlayerId, labelStyle);
        GUILayout.Label("是否为房主: " + entry.IsHost, labelStyle);

        GUILayout.Space(6f);

        GUILayout.Label("Send BattleState: " + entry.SendBattleStateCount, labelStyle);
        GUILayout.Label("Receive BattleState: " + entry.ReceiveBattleStateCount, labelStyle);
        GUILayout.Label("Receive BattleHit: " + entry.ReceiveBattleHitCount, labelStyle);
        GUILayout.Label("Receive BattleDeath: " + entry.ReceiveBattleDeathCount, labelStyle);

        GUILayout.Space(6f);

        GUILayout.Label("Remote Snapshot Objects: " + entry.RemoteSnapshotObjectCount, labelStyle);
        GUILayout.Label("Interpolation BackTime: " + entry.SnapshotInterpolationBackTime.ToString("F3"), labelStyle);

        GUILayout.Space(6f);

        GUILayout.Label("Registered Players: " + entry.RemotePlayerCount, labelStyle);
        GUILayout.Label("Registered Monsters: " + entry.MonsterCount, labelStyle);

        GUILayout.Space(6f);

        GUILayout.Label("Send BattleDestroy: " + entry.SendBattleDestroyCount, labelStyle);
        GUILayout.Label("Receive BattleDestroy: " + entry.ReceiveBattleDestroyCount, labelStyle);

        GUILayout.Space(6f);
        GUILayout.Label("Toggle: F3", labelStyle);

        GUI.DragWindow();
    }
}