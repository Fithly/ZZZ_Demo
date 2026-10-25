using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor; //使用Editor要放在Editor目录下
using System.IO;

/// <summary>
/// AB 包导出工具
/// 
/// 支持：
/// 1. 导出到 DataPath/AB，用于编辑器调试
/// 2. 导出到 StreamingAssets/AB，用于打包首包
/// </summary>
public class ExportAB
{
    [MenuItem("AB包/导出到DataPath/Windows")]
    public static void ExportDataPathForWindows()
    {
        Export(BuildTarget.StandaloneWindows, GetEditorDataPathABPath());
    }

    [MenuItem("AB包/导出到StreamingAssets/Windows")]
    public static void ExportStreamingAssetsForWindows()
    {
        Export(BuildTarget.StandaloneWindows, GetStreamingAssetsABPath());
    }

    [MenuItem("AB包/导出到DataPath/Android")]
    public static void ExportDataPathForAndroid()
    {
        Export(BuildTarget.Android, GetEditorDataPathABPath());
    }

    [MenuItem("AB包/导出到StreamingAssets/Android")]
    public static void ExportStreamingAssetsForAndroid()
    {
        Export(BuildTarget.Android, GetStreamingAssetsABPath());
    }

    [MenuItem("AB包/导出到DataPath/Mac")]
    public static void ExportDataPathForMac()
    {
        Export(BuildTarget.StandaloneOSX, GetEditorDataPathABPath());
    }

    [MenuItem("AB包/导出到StreamingAssets/Mac")]
    public static void ExportStreamingAssetsForMac()
    {
        Export(BuildTarget.StandaloneOSX, GetStreamingAssetsABPath());
    }

    // 编辑器调试目录：项目根目录/DataPath/AB
    private static string GetEditorDataPathABPath()
    {
        string projectRoot = Application.dataPath.Substring(0, Application.dataPath.Length - 7);
        return Path.Combine(projectRoot, "DataPath/AB");
    }

    // 首包目录：Assets/StreamingAssets/AB
    private static string GetStreamingAssetsABPath()
    {
        return Path.Combine(Application.dataPath, "StreamingAssets/AB");
    }

    //ab包资源不能放在Resources文件夹下 ab包是独立出去的
    private static void Export(BuildTarget platform, string outputPath)
    {
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        Debug.Log($"[ExportAB] 输出路径={outputPath}");

        //导出ab包的核心代码，生成ab包文件
        //参数1：ab包文件存储路径
        //参数2：导出选项
        //参数3：平台（不同平台的ab包是不一样的）
        BuildPipeline.BuildAssetBundles(
            outputPath,
            //不压缩 且记录依赖关系 且强制全部重新导出ab包（把老的覆盖了）
            BuildAssetBundleOptions.UncompressedAssetBundle | BuildAssetBundleOptions.ForceRebuildAssetBundle,
            platform
        );

        Debug.Log("导出ab包成功");
    }
}
