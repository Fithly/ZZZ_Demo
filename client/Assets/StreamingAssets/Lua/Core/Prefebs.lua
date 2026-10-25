-- 全局表 用表来封装函数
Prefebs = {}

-- 预制体存储路径
function Prefebs:Instantiate(prefeb)
    -- 实例化预制体
    local go = CS.UnityEngine.Object.Instantiate(prefeb)
    go.name = prefeb.name

    local canvas = CS.UnityEngine.GameObject.Find("/Canvas")

    -- 更改父物体 和调整transform
    local trs = go.transform
    go.transform:SetParent(canvas.transform)
    trs.localPosition = CS.UnityEngine.Vector3.zero
    trs.localRotation = CS.UnityEngine.Quaternion.identity
    trs.localScale = CS.UnityEngine.Vector3.one

    -- 四锚点边距归零 (offsetMin自动识别转换成RectTransform)
    trs.offsetMin = CS.UnityEngine.Vector2.zero
    trs.offsetMax = CS.UnityEngine.Vector2.zero

    return go
end