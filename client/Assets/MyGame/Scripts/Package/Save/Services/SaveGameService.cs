using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 创建新存档
/// 保存到 Json
/// 从 Json 加载
/// 自动迁移
/// 出错时兜底默认存档
/// </summary>
public class SaveGameService
{
    // 创建当前账号的新存档
    public GameSaveData CreateNewSaveForCurrentAccount()
    {
        if (!CurrentAccountSession.IsLoggedIn)
            return CreateNewSave();

        GameSaveData saveData = GameSaveData.CreateDefaultForAccount(
            CurrentAccountSession.AccountId,
            CurrentAccountSession.AccountName);

        saveData = SaveMigrationService.MigrateAndRepair(saveData);
        return saveData;
    }

    // 保存当前账号存档
    public void SaveForCurrentAccount(GameSaveData saveData, bool prettyPrint = true)
    {
        string fileName = AccountSaveFileNameUtility.GetCurrentAccountSaveFileName();
        Save(saveData, fileName, prettyPrint);
    }

    // 加载或创建当前账号存档
    public GameSaveData LoadOrCreateForCurrentAccount()
    {
        string fileName = AccountSaveFileNameUtility.GetCurrentAccountSaveFileName();

        if (!Exists(fileName))
        {
            Debug.Log($"[SaveGameService] 当前账号存档不存在，创建新存档. AccountId={CurrentAccountSession.AccountId}");

            GameSaveData newSave = CreateNewSaveForCurrentAccount();
            Save(newSave, fileName);
            return newSave;
        }

        GameSaveData loaded = LoadOrCreate(fileName);

        // 防止旧存档里 PlayerId / PlayerName 为空，或者仍然是默认 Player。
        if (CurrentAccountSession.IsLoggedIn && loaded != null)
        {
            loaded.PlayerBaseData.PlayerId = CurrentAccountSession.AccountId;
            loaded.PlayerBaseData.PlayerName = CurrentAccountSession.AccountName;
        }

        return loaded;
    }

    // 删除当前账号存档
    public void DeleteCurrentAccountSave()
    {
        string fileName = AccountSaveFileNameUtility.GetCurrentAccountSaveFileName();
        Delete(fileName);
    }

    // 读取当前账号原始 Json
    public string ReadCurrentAccountRawJson()
    {
        string fileName = AccountSaveFileNameUtility.GetCurrentAccountSaveFileName();
        return ReadRawJson(fileName);
    }

    public GameSaveData CreateNewSave()
    {
        return SaveMigrationService.CreateFreshSave();
    }

    public void Save(GameSaveData saveData, string fileName = null, bool prettyPrint = true)
    {
        if (saveData == null)
            throw new ArgumentNullException(nameof(saveData));

        saveData = SaveMigrationService.MigrateAndRepair(saveData);
        saveData.SaveVersion = SaveGameConstants.CurrentSaveVersion;
        saveData.SaveTimeTicks = DateTime.UtcNow.Ticks;

        // 序列化
        string json = JsonUtility.ToJson(saveData, prettyPrint);
        string path = SavePathUtility.GetSaveFilePath(fileName);

        // 写入
        File.WriteAllText(path, json);
        Debug.Log($"[SaveGameService] 保存成功. 路径={path}");
    }

    // 加载或者创建
    public GameSaveData LoadOrCreate(string fileName = null)
    {
        string path = SavePathUtility.GetSaveFilePath(fileName);

        // 路径不存在
        if (!File.Exists(path))
        {
            Debug.Log($"[SaveGameService] 保存文件不存在. 创建默认存档. 路径={path}");
            return CreateNewSave();
        }

        try
        {
            // 读取
            string json = File.ReadAllText(path);

            // json文件不为空
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning($"[SaveGameService] Json文件为空. 创建默认存档. 路径={path}");
                return CreateNewSave();
            }

            // 反序列化
            var loaded = JsonUtility.FromJson<GameSaveData>(json);
            // 迁移和修复
            loaded = SaveMigrationService.MigrateAndRepair(loaded);

            Debug.Log($"[SaveGameService] 加载成功. 路径={path}");
            return loaded;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveGameService] 加载失败. 路径={path}, 错误={ex}");
            return CreateNewSave();
        }
    }

    public bool Exists(string fileName = null)
    {
        string path = SavePathUtility.GetSaveFilePath(fileName);
        return File.Exists(path);
    }

    // 删除存档
    public void Delete(string fileName = null)
    {
        string path = SavePathUtility.GetSaveFilePath(fileName);

        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[SaveGameService] 删除存档. 路径={path}");
        }
    }

    // 读取原始Json
    public string ReadRawJson(string fileName = null)
    {
        string path = SavePathUtility.GetSaveFilePath(fileName);

        if (!File.Exists(path))
            return string.Empty;

        return File.ReadAllText(path);
    }
}