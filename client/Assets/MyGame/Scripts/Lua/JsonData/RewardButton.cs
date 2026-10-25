using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RewardButton : MonoBehaviour
{
    [SerializeField] private Button rewardBtn;
    private LuaDemoBridge luaDemoBridge;

    private void Start()
    {
        rewardBtn.onClick.RemoveAllListeners();
        rewardBtn.onClick.AddListener(GetReward);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F4))
        {
            GetReward();
        }
    }

    public void Initialize(LuaDemoBridge luaDemoBridge)
    {
        this.luaDemoBridge = luaDemoBridge;
    }

    private void GetReward()
    {
        if (luaDemoBridge == null)
        {
            Debug.LogWarning("[RewardButton] LuaDemoBridge is null.");
            return;
        }

        luaDemoBridge.CallLuaFunction("OnLoginReward");

        GameUIEntry uiEntry = FindObjectOfType<GameUIEntry>();
        if (uiEntry != null)
            uiEntry.ReloadAndRefreshFromSave();
    }
}
