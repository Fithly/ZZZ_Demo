local RoleDataModel = require("DataModel/RoleDataModel")
local RoleView = require("View/RoleView")

local MainMenuController = {}

function MainMenuController:New()
    local obj = {}
    obj.view = nil
    obj.model = nil
    obj.pageGameObject = nil

    setmetatable(obj, self)
    self.__index = self

    return obj
end

-- 控制器一被加载时 Start生命周期函数执行
-- Lua模拟Start周期函数
function MainMenuController:Start()
    -- 获取AB包文件
    ABManager:LoadFile("mainmenu")
    local obj = ABManager:LoadAsset("mainmenu", "MainMenu", typeof(CS.UnityEngine.Object))
    -- 加载预制体
    self.pageGameObject = Prefebs:Instantiate(obj)

    -- 初始化视图 传入UI实例
    self.view = RoleView:New(self.pageGameObject)
    -- 绑定按钮点击回调
    self.view:Initialize(function ()
        -- 当按钮被点击时 执行控制器中的方法 通知数据模型修改数据
        self:OnAddButtonClicked()
    end)

    -- 读取数据模型
    self.model = RoleDataModel:New()
    -- 监听模型数据变化 并自动更新视图
    self.model.onCountChanged = function (data)
        self.view:UpdateCountDisplay(data)
    end

    -- 初始化显示
    self.view:UpdateCountDisplay(self.model:GetAllData())
end

-- 修改模型数据
function MainMenuController:OnAddButtonClicked()
    -- 控制器只调用模型的方法 不处理Json或UI更新细节
    self.model:AddGold(10)
end

-- -- 每帧都会被调用
-- function Controller:Update()
--     print("MainMenuUpdate()")
-- end

return MainMenuController