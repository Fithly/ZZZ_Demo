local Config = {}

-- 文案配置
Config.Texts = {
    LoginRewardSuccess = "登录奖励领取成功",
    RewardAlreadyClaimed = "这个账号已经领取过登录奖励",
    TestGrantSuccess = "Lua 测试发物成功",
    RewardConfigMissing = "Lua 奖励配置不存在",
    RewardItemInvalid = "Lua 奖励物品配置错误",
    RewardAllItemsAlreadyOwned = "测试发物跳过：背包中已经存在这些物品",
    StrengthenSuccess = "强化成功：Lv.{0} → Lv.{1}"
}

-- 奖励配置
Config.Rewards = {
    -- 登录奖励，只领取一次
    -- 注意：
    -- 登录奖励不应该检查背包里是否已有该物品。
    -- 只要这个奖励ID没领取过，就正常发放。
    LoginReward_001 = {
        Once = true,
        SkipExistingItems = false,
        SuccessTextKey = "LoginRewardSuccess",
        Items = {
            { ItemId = 10002, Count = 1 }
        }
    },

    -- 测试发物，可以反复执行
    -- 注意：
    -- 测试发物才需要跳过背包里已经存在的物品，避免每次运行重复发。
    TestBackpackGrant_001 = {
        Once = false,
        SkipExistingItems = true,
        SuccessTextKey = "TestGrantSuccess",
        Items = {
            { ItemId = 10001, Count = 1 },
            { ItemId = 20001, Count = 1 },
            { ItemId = 20002, Count = 1 },
            { ItemId = 20003, Count = 1 },
            { ItemId = 20004, Count = 1 },
            { ItemId = 20005, Count = 1 },
            { ItemId = 20006, Count = 1 },
            { ItemId = 20007, Count = 1 },
            { ItemId = 20008, Count = 1 },
            { ItemId = 20009, Count = 1 },
            { ItemId = 20010, Count = 1 },
            { ItemId = 20011, Count = 1 },
            { ItemId = 20012, Count = 1 },
            { ItemId = 30001, Count = 99 },
            { ItemId = 30002, Count = 99 },
            { ItemId = 30003, Count = 99 },
            { ItemId = 30004, Count = 99 },
            { ItemId = 30005, Count = 99 },
            { ItemId = 30006, Count = 99 },
            { ItemId = 30007, Count = 99 },
            { ItemId = 30008, Count = 99 },
            { ItemId = 30009, Count = 99 }
        }
    }
}

-- 登录时由 C# 调用
function Config.OnLoginReward(bridge)
    bridge:GrantRewardById("LoginReward_001")
end

-- 测试按钮 / 测试入口调用
function Config.OnTestBackpackGrant(bridge)
    bridge:GrantRewardById("TestBackpackGrant_001")
end

return Config