using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketGameProtocol;

public class BaseRequest : MonoBehaviour
{
    // 消息类型
    protected RequestCode requestCode;
    protected ActionCode actionCode;

    // 单例
    protected GameFace face;

    public ActionCode GetActionCode
    {
        get
        {
            return actionCode;
        }
    }

    public virtual void Awake()
    {
        face = GameFace.Instance;
    }

    public virtual void Start()
    {
        // 添加请求
        face.AddRequest(this);
        //Debug.Log("添加" + actionCode.ToString());
    }

    public virtual void OnDestroy()
    {
        face.RemoveRequest(actionCode);
    }

    // 接收
    public virtual void OnResponse(MainPack pack)
    {

    }

    // 发送
    public virtual void SendRequest(MainPack pack)
    {
        face.Send(pack);
    }
}
