using System;

/// <summary>
/// 网格显示层的数据模型
///
/// 关键点是保存“显示项”和“真实背包槽位”的映射
/// 
/// 后面点击筛选后的第 N 个 slot，仍能找到真实源槽位
/// </summary>
[Serializable]
public class InventoryDisplayItem
{
    public int SourceSlotIndex;
    public InventorySlotData SourceSlotData;
    public ItemInstanceData ItemInstance;
    public ItemDefinition ItemDefinition;
}