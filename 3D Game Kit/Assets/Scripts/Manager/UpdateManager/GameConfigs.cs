using UnityEngine;
using QF.Res;
namespace QF.UpdateMgr
{
    public enum DevelopMode
    {
        /// <summary>
        /// 编辑器下开发
        /// </summary>
        EditorDevelop,
        /// <summary>
        /// 打包线下测试
        /// </summary>
        LocalRelease,
        /// <summary>
        /// 发布测试
        /// </summary>
        VirtualLaunch,
        /// <summary>
        /// 上线发布
        /// </summary>
        Release
    }
    /// <summary>
    /// 游戏配置表
    /// ResKitUtil.GetPlatformName()这个位置可跟踪到 程序打包后加载app资源路径
    /// 打包后 解压资源路径必须与 ResKit里面跟踪的路径一致 否则会出现加载不到资源的问题
    /// </summary>
    public static class GameConfigs
    {
        public static DevelopMode developMode = DevelopMode.EditorDevelop;
        //--------------------------------------------平台------------------------------------------------------------
        /// <summary>
        /// 当前平台名
        /// </summary>
        public static string CurPlatformName { get { return ResKitUtil.GetPlatformName(); } }
        //--------------------------------------------上线远端路径---------------------------------------------------------
        /// <summary>
        /// 服务器版本号url
        /// </summary>
        public static string ServerVersionUrl = "http://127.0.0.1/ResServer/version.txt";
        /// <summary>
        /// 资源服务器url
        /// </summary>
        public static string ResServerUrl = "http://127.0.0.1/ResServer";
        /// <summary>
        /// 资源服务器ab包根路径
        /// </summary>
        public static string OnlineABRootPath = ResServerUrl + "/AssetBundles/" + CurPlatformName;
        /// <summary>
        /// 资源服务器manifest文件路径
        /// </summary>
        public static string OnlineManifestPath = ResServerUrl + "/AssetBundles/" + CurPlatformName;
        //--------------------------------------------测试远端路径---------------------------------------------------------
        /// <summary>
        /// Virtual服务器版本号url
        /// </summary>
        public static string VirtualServerVersionUrl = "http://127.0.0.1/VirtualResServer/version.txt";
        /// <summary>
        /// Virtual资源服务器url
        /// </summary>
        public static string VirtualResServerUrl = "http://127.0.0.1/VirtualResServer";
        /// <summary>
        /// Virtual资源服务器ab包根路径
        /// </summary>
        public static string OnlineVirtualABRootPath = VirtualResServerUrl + "/AssetBundles/" + CurPlatformName;
        /// <summary>
        /// Virtual资源服务器manifest文件路径
        /// </summary>
        public static string OnlineVirtualManifestPath = VirtualResServerUrl + "/AssetBundles/" + CurPlatformName;
        //---------------------------------------------包体路径----------------------------------------------------------
        /// <summary>
        /// 游戏资源文件路径
        /// </summary>
        public static string GameResPath = Application.dataPath;
        /// <summary>
        /// 临时数据存放 缓存
        /// </summary>
        public static string TmpPath = Application.temporaryCachePath + "/Cache/" + CurPlatformName;
        /// <summary>
        /// (该文件夹只能读,打包时被一起写入包内,第一次运行游戏把该文件夹数据拷贝到本地ab包路径下) 
        /// </summary>
        public static string StreamingAssetABRootPath = Application.streamingAssetsPath + "/AssetBundles/" + CurPlatformName;
        /// <summary>
        /// streamingasset目录下的manifest文件路径
        /// </summary>
        public static string StreamingAssetManifestPath = Application.streamingAssetsPath + "/AssetBundles/" + CurPlatformName;
        //---------------------------------------------解压后路径----------------------------------------------------------
        /// <summary>
        /// 打包资源的输出文件夹
        /// </summary>
        public static string GameResExportPath = Application.streamingAssetsPath + "/AssetBundles/" + CurPlatformName;
        /// <summary>
        /// 本地ab包根路径(该文件夹可读可写,从资源服务器更新的数据也放在这里)
        /// </summary>
        public static string LocalABRootPath = Application.persistentDataPath + "/AssetBundles/" + CurPlatformName;
        /// <summary>
        /// 本地manifest文件路径
        /// </summary>
        public static string LocalManifestPath = Application.persistentDataPath + "/AssetBundles/" + CurPlatformName;

    }
}