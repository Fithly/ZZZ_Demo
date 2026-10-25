ABManager = {}

function ABManager:Init()
    CS.AssetBundleManager.Instance:Initialize()
end

-- Lua 不再直接管理底层 AB 依赖，只调用 C# 的 AssetBundleManager
function ABManager:LoadAsset(bundleName, assetName, assetType)
    if assetType == nil then
        assetType = typeof(CS.UnityEngine.Object)
    end

    return CS.AssetBundleManager.Instance:LoadAsset(bundleName, assetName, assetType)
end

-- 初始化
function ABManager:Instantiate(bundleName, assetName, parentPath)
    local prefab = self:LoadAsset(bundleName, assetName, typeof(CS.UnityEngine.GameObject))
    if prefab == nil then
        print("[ABManager] 预制体加载失败: " .. bundleName .. "/" .. assetName)
        return nil
    end

    -- 生成预制体
    local go = CS.UnityEngine.Object.Instantiate(prefab)
    go.name = prefab.name

    local parent = nil
    if parentPath ~= nil and parentPath ~= "" then
        parent = CS.UnityEngine.GameObject.Find(parentPath)
    end

    if parent == nil then
        parent = CS.UnityEngine.GameObject.Find("/Canvas")
    end

    if parent ~= nil then
        local trs = go.transform
        trs:SetParent(parent.transform)
        trs.localPosition = CS.UnityEngine.Vector3.zero
        trs.localRotation = CS.UnityEngine.Quaternion.identity
        trs.localScale = CS.UnityEngine.Vector3.one

        local rect = go:GetComponent(typeof(CS.UnityEngine.RectTransform))
        if rect ~= nil then
            rect.offsetMin = CS.UnityEngine.Vector2.zero
            rect.offsetMax = CS.UnityEngine.Vector2.zero
        end
    end

    return go
end

function ABManager:Unload(bundleName)
    CS.AssetBundleManager.Instance:UnloadBundle(bundleName, false)
end

return ABManager