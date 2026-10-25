local RoleDataModel = {}

-- 放在可写目录下
-- 数据文件的路径 应完全由Model内部管理
local DATA_PATH = CS.UnityEngine.Application.persistentDataPath .. "/Role.json"

function RoleDataModel:New()
    -- 创建新表作为对象实例
    local obj = {}
    -- 私有数据
    obj._gold = 0                
    obj._diamond = 0
    obj.onCountChanged = nil    -- 回调函数（代替事件通知View更新）

    -- 设置元表 让obj能访问RoleDataModle的所有方法
    -- 设置索引为自身 就能找到RoleDataModle的方法
    setmetatable(obj, self)
    self.__index = self

    print(RoleDataModel.Path)

    -- 构造时自动从磁盘加载数据
    obj:LoadFromDisk()

    return obj
end

-- 从磁盘（文件）加载数据
function RoleDataModel:LoadFromDisk()
    if(CS.System.IO.File.Exists(DATA_PATH))
    then
        -- 解析已有的Json文件 并加载更新里面的数据
        local jsonTest = CS.System.IO.File.ReadAllText(DATA_PATH)
        local data = LuaJson.decode(jsonTest) 
        self._gold = data.Gold or self._gold
        self._diamond = data.Diamond or self._diamond
    else
        -- 文件不存在 用默认值保存
        self:_SaveToDisk()
    end
end

-- 将数据存入磁盘
function RoleDataModel:_SaveToDisk()
    local data = {
        Gold = self._gold,
        Diamond = self._diamond
    }
    -- 序列化为Json文件 写入Json中
    local jsonText = LuaJson.encode(data)
    CS.System.IO.File.WriteAllText(DATA_PATH, jsonText)
end

-- 对外的数据接口 不暴露Json细节
function RoleDataModel:AddGold(amount)
    self._gold = self._gold + amount
    self:_SaveToDisk() -- 自动更新保存
    -- 通知订阅者数据已变更
    if self.onCountChanged then
        self.onCountChanged({Gold = self._gold, Diamond = self._diamond}) -- onCountChanged?.Invoke(带参)
    end
    return self._gold
end

function RoleDataModel:GetGold()
    return self._gold
end

function RoleDataModel:GetDiamond()
    return self._diamond
end

function RoleDataModel:GetAllData()
    return {Gold = self._gold, Diamond = self._diamond}
end

return RoleDataModel