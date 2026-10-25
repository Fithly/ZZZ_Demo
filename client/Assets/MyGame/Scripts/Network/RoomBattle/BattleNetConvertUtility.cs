using SocketGameProtocol;
using UnityEngine;

/// <summary>
/// 战斗网络数据转换工具
/// 
/// 职责：
/// 1. protobuf Transform -> Unity Vector3 / Quaternion
/// 2. Unity Transform -> protobuf Transform
/// 3. 保持 NetworkBattleEntry 中的代码简洁
/// </summary>
public static class BattleNetConvertUtility
{
    public static Vector3 ToVector3(BattleTransformPack pack)
    {
        if (pack == null)
            return Vector3.zero;

        return new Vector3(pack.PosX, pack.PosY, pack.PosZ);
    }

    public static Quaternion ToQuaternion(BattleTransformPack pack)
    {
        if (pack == null)
            return Quaternion.identity;

        return Quaternion.Euler(0f, pack.RotY, 0f);
    }

    public static BattleTransformPack ToBattleTransformPack(Vector3 pos, Quaternion rot)
    {
        BattleTransformPack pack = new BattleTransformPack();

        pack.PosX = pos.x;
        pack.PosY = pos.y;
        pack.PosZ = pos.z;
        pack.RotY = rot.eulerAngles.y;

        return pack;
    }

    public static BattleTransformPack ToBattleTransformPack(Transform trans)
    {
        if (trans == null)
            return new BattleTransformPack();

        return ToBattleTransformPack(trans.position, trans.rotation);
    }
}