using System;

/// <summary>
/// Lua 奖励领取记录
/// 
/// 用于保存到 GameSaveData 中。
/// 
/// RewardId：Lua 配置里的奖励ID
/// Once：领取时该奖励是否为一次性奖励
/// ClaimCount：领取次数
/// LastClaimTimeTicks：最后领取时间
/// </summary>
[Serializable]
public class LuaRewardClaimData
{
    public string RewardId;
    public bool Once;
    public int ClaimCount;
    public long LastClaimTimeTicks;
}