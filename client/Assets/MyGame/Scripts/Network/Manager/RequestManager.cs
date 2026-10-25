using SocketGameProtocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ClientManager 负责“收到包”
/// RequestManager 负责“这个包该谁处理”
/// </summary>
public class RequestManager : BaseManager
{
    public RequestManager(GameFace face) : base(face) { }

    private Dictionary<ActionCode, BaseRequest> requestDict = new Dictionary<ActionCode, BaseRequest>();

    public void AddRequest(BaseRequest request)
    {
        if (request == null)
            return;

        ActionCode action = request.GetActionCode;

        // 避免重复 Add 直接报错
        // 场景切换、重复挂载、旧对象未及时销毁时，都以最新 Request 为准
        if (requestDict.ContainsKey(action))
        {
            Debug.LogWarning("[RequestManager] 重复注册 ActionCode，已覆盖：" + action);
            requestDict[action] = request;
        }
        else
        {
            requestDict.Add(action, request);
        }
    }

    public void RemoveRequest(ActionCode action)
    {
        // 先判断再删除，避免重复销毁时报错
        if (requestDict.ContainsKey(action))
        {
            requestDict.Remove(action);
        }
    }

    public void HandleResponse(MainPack pack)
    {
        if (requestDict.TryGetValue(pack.Actioncode, out BaseRequest request))
        {
            request.OnResponse(pack);
        }
        else
        {
            Debug.LogWarning("找不到对应处理");
        }
    }
}
