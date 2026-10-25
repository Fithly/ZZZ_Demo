using System;

// 批量操作结果
[Serializable]
public class EquipmentBatchOperationResult
{
    public bool Success;
    public int RemovedCount;
    public string ErrorMessage;

    public static EquipmentBatchOperationResult Ok(int removedCount = 0)
    {
        return new EquipmentBatchOperationResult
        {
            Success = true,
            RemovedCount = removedCount
        };
    }

    public static EquipmentBatchOperationResult Fail(string error)
    {
        return new EquipmentBatchOperationResult
        {
            Success = false,
            ErrorMessage = error
        };
    }
}