using System.Collections.Generic;

/// <summary>
/// 项目实例属性工具
/// </summary>
public static class ItemInstanceAttributeUtility
{
    // 根据Key获取属性值
    public static float GetValue(List<ItemAttributeData> attributes, string key)
    {
        if (attributes == null || string.IsNullOrEmpty(key))
            return 0f;

        for (int i = 0; i < attributes.Count; i++)
        {
            if (attributes[i].Key == key)
                return attributes[i].Value;
        }

        return 0f;
    }

    // 根据Key设置属性值
    public static void SetValue(List<ItemAttributeData> attributes, string key, float value)
    {
        if (attributes == null || string.IsNullOrEmpty(key))
            return;

        for (int i = 0; i < attributes.Count; i++)
        {
            if (attributes[i].Key == key)
            {
                var data = attributes[i];
                data.Value = value;
                attributes[i] = data;
                return;
            }
        }

        // 如果找不到对应属性则新增
        attributes.Add(new ItemAttributeData
        {
            Key = key,
            Value = value
        });
    }

    // 移除某项属性
    public static void RemoveValue(List<ItemAttributeData> attributes, string key)
    {
        if (attributes == null || string.IsNullOrEmpty(key))
            return;

        for (int i = attributes.Count - 1; i >= 0; i--)
        {
            if (attributes[i].Key == key)
                attributes.RemoveAt(i);
        }
    }

    // 清除全部属性
    public static void ClearAll(List<ItemAttributeData> attributes)
    {
        if (attributes == null)
            return;

        attributes.Clear();
    }
}