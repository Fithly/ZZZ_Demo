using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UserItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerName;

    public void SetPlayerInfo(string name)
    {
        playerName.text = name;
    }
}
