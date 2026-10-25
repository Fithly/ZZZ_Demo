using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasePanel : MonoBehaviour
{
    protected NetworkUIManager uiManager;
    public NetworkUIManager _setUIManager
    {
        set
        {
            uiManager = value;
        }
    }


    public virtual void OnEnter()
    {

    }

    public virtual void OnPause()
    {

    }

    public virtual void OnRecovery()
    {

    }

    public virtual void OnExit()
    {

    }
}
