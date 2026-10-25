local RoleView = {}

function RoleView:New(uiRootGameObject)
    local obj = {}
    obj.gameObject = uiRootGameObject -- 保留组件根目录引用
    obj.goldText = nil -- 文本组件
    obj.diamondText = nil -- 文本组件
    obj.addButton = nil -- 按钮组件

    -- 设置元表
    setmetatable(obj, self)
    self.__index = self

    return obj
end

-- 初始化方法 绑定按钮事件
function RoleView:Initialize(onClickAddButton)
    -- 基于传入的根目录获取UI组件
    local trs = self.gameObject.transform
    self.goldText = trs:Find("HeaderCount/Gold/Count"):GetComponent(typeof(CS.UnityEngine.UI.Text))
    self.diamondText = trs:Find("HeaderCount/Diamond/Count"):GetComponent(typeof(CS.UnityEngine.UI.Text))
    self.addButton = trs:Find("HeaderCount/Gold/Add"):GetComponent(typeof(CS.UnityEngine.UI.Button))

    -- 绑定按钮点击事件
    self.addButton.onClick:AddListener(function ()
        -- 当按钮被点击时 调用传入的回调函数
        if onClickAddButton then
            onClickAddButton()
        end
    end)
end

-- 接受一个简单的数据表更新 不涉及Json
function RoleView:UpdateCountDisplay(data)
    if(self.goldText) then
        self.goldText.text = tostring(data.Gold)
    end
    if(self.diamondText) then
        self.diamondText.text = tostring(data.Diamond)
    end
end

return RoleView