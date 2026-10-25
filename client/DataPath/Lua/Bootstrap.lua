Bootstrap = {}

-- 注册总表 用来存储所有控制器
Bootstrap.Controllers = {}

Bootstrap.Start = function()
    Bootstrap.LoadPage("MainMenuController")
end

Bootstrap.Update = function()
    -- 迭代器遍历 调用所有控制器的Update方法
    for k, v in pairs(Bootstrap.Controllers)
    do
        -- 判断是否有实现Update
        if (v.Update ~= nil) then
            v:Update() 
        end
    end
end

Bootstrap.OnDestroy = function()
    print("Lua OnDestroy")
end

-- 提供一个核心table函数用于加载页面控制器
-- 参数:脚本名称
Bootstrap.LoadPage = function (name)
    -- 加载Controller目录下对应的脚本
    local ControllerClass = require("Controller/" .. name)
    local controllerInstance = ControllerClass:New()
    -- 注册给核心的table 这样生命周期函数就会正常调用
    Bootstrap.Controllers[name] = controllerInstance

    -- 加载页面时 同时运行Start
    controllerInstance:Start()
end

require("Config")
ABManager = require("Core/ABManager")
ABManager:Init()
require("Core/Prefebs")

-- 将Json开辟为一个全局变量
LuaJson = require("LuaJson")
-- LuaJson.encode()
-- LuaJson.decode()


-- -- 使用表封装的函数
-- require("Prefebs")
-- Prefebs:Load("Test")

-- -- json库
-- local json = require("LuaJson")
-- -- json解析出表table
-- local data = json.decode('{"Name" : "admin", "ID" : 100}')
-- print(data.Name)
-- local data1 = json.decode('[1, 2, 3]')
-- print(data1[1])
