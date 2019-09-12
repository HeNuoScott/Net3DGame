using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.Events;
using UnityEngine;
using System.IO;

namespace QF.UpdateMgr
{
    [MonoSingletonPath("[Framework]/UpdateAssetManager")]
    public class UpdateAssetManager : MonoSingleton<UpdateAssetManager>
    {
        private AssetBundleManifest curManifest;
        private AssetBundleManifest onlineManifest;

        /// <summary>
        /// 检测资源
        /// </summary>
        /// <param name="onComplete"></param>
        public void CheckAsset(UnityAction onComplete = null)
        {
            if (GameConfigs.developMode == DevelopMode.EditorDevelop)
            {
                //如果是开发者模式  直接返回 
                onComplete?.Invoke();
            }
            else
            {
                StartCoroutine(Progress(onComplete));
            }
        }

        /// <summary>
        /// 检测资源过程
        /// </summary>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        private IEnumerator Progress(UnityAction onComplete)
        {
            //第一次进入游戏 把streamingassets文件夹数据解压缩到指定的下载目录
            if (true || PlayerPrefs.GetString("IsFirstLaunch", "true") == "true")
                yield return StartCoroutine(StreamingAssetfolderCopyToDownloadFolder());

            if (GameConfigs.developMode == DevelopMode.LocalRelease)
            {
                onComplete?.Invoke();
                yield break; 
            }

            // 加载本地 manifest文件
            if (File.Exists(GameConfigs.LocalManifestPath))
            {
                var manifestAB = AssetBundle.LoadFromFile(GameConfigs.LocalManifestPath);
                curManifest = manifestAB.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                manifestAB.Unload(false);
            }
            else Debug.Log("本地资源文件丢失:" + GameConfigs.LocalManifestPath);

            string OnlineManifestPath = string.Empty;
            if (GameConfigs.developMode == DevelopMode.VirtualLaunch)
            {
                OnlineManifestPath = GameConfigs.OnlineVirtualManifestPath;
            }
            else
            {
                OnlineManifestPath = GameConfigs.OnlineManifestPath;
            }

            //获取资源服务器端manifest  检测是否更新资源...
            Debug.Log("获取资源服务器资源manifest :" + OnlineManifestPath);
            UnityWebRequest webReq = UnityWebRequest.Get(OnlineManifestPath);
            yield return webReq.SendWebRequest();

            if (webReq.isNetworkError || webReq.isHttpError)
            {
                Debug.Log(webReq.error);
            }
            else
            {
                if (webReq.responseCode == 200)
                {
                    byte[] result = webReq.downloadHandler.data;
                    AssetBundle onlineManifestAB = AssetBundle.LoadFromMemory(result);
                    onlineManifest = onlineManifestAB.LoadAsset<AssetBundleManifest>("AssetBundlemanifest");
                    onlineManifestAB.Unload(false);
                    //更新本地manifest
                    WriteFile(GameConfigs.LocalManifestPath, webReq.downloadHandler.data);
                }
                yield return StartCoroutine(Download());

                onComplete?.Invoke();
            }

        }

        /// <summary>
        /// streamingAsset文件夹数据解压缩到下载文件夹
        /// </summary>
        /// <returns></returns>
        private IEnumerator StreamingAssetfolderCopyToDownloadFolder()
        {
            Debug.Log("初次运行,解压缩包数据到本地下载文件夹!");
            string srcmanifestpath = GameConfigs.StreamingAssetManifestPath;

            // 方法一
            if (Directory.Exists(GameConfigs.GameResExportPath))
            {
                Debug.Log("存在:" + GameConfigs.GameResExportPath);

                //获取该文件夹下所有文件(包含子文件夹)
                var list = PathUtils.GetFilesPath(GameConfigs.GameResExportPath);
                int total = list.Length;
                int count = 0;
                foreach (var iter in list)
                {
                    //原路径
                    string srcPath = iter;
                    //目标路径
                    string tarPath = iter.Replace(GameConfigs.GameResExportPath, GameConfigs.LocalABRootPath);
                    //用 UnityWebRequest
                    UnityWebRequest req = UnityWebRequest.Get(srcmanifestpath);
                    yield return req.SendWebRequest();

                    if (req.isNetworkError || req.isHttpError)
                    {
                        Debug.Log(req.error);
                    }
                    else
                    {   //查看目标路径是否存在如果存在删除
                        if (File.Exists(tarPath))
                        {
                            File.Delete(tarPath);
                        }
                        else//如果不存在创建新的路径
                        {
                            PathUtils.CreateFolderByFilePath(tarPath);
                        }
                        //FileStream fs2 = File.Create(tarPath);
                        //fs2.Write(req.downloadHandler.data, 0, req.downloadHandler.data.Length);
                        //fs2.Flush();
                        //fs2.Close();
                        //用文件流的方式将需要解压的文件写入目标路径
                        using (FileStream fs2 = File.Create(tarPath))
                        {
                            fs2.Write(req.downloadHandler.data, 0, req.downloadHandler.data.Length);
                            fs2.Flush();
                        }
                        Debug.LogFormat("->解压缩文件{0}到{1}成功", srcPath, tarPath);
                    }
                    count++;
                }

            }
            else Debug.Log("无需解压缩!");

            #region 方法二
            //// way 2
            //if (File.Exists(srcmanifestpath))
            //{
            //    Debug.Log("存在:" + srcmanifestpath);

            //    UnityWebRequest req = UnityWebRequest.Get(srcmanifestpath);
            //    yield return req.SendWebRequest();

            //    if (req.isNetworkError)
            //    {
            //        Debug.Log(req.error);
            //    }
            //    else
            //    {
            //        string tarmanifestpath = GameConfigs.LocalManifestPath;

            //        // copy manifest file
            //        if (File.Exists(tarmanifestpath))
            //        {
            //            File.Delete(tarmanifestpath);
            //        }
            //        else
            //        {
            //            PathUtils.CreateFolderByFilePath(tarmanifestpath);
            //        }
            //        FileStream fs2 = File.Create(tarmanifestpath);
            //        fs2.Write(req.downloadHandler.data, 0, req.downloadHandler.data.Length);
            //        fs2.Flush();
            //        fs2.Close();
            //        Debug.LogFormat("解压缩文件{0}到{1}成功", srcmanifestpath, tarmanifestpath);



            //        var manifestAB = AssetBundle.LoadFromMemory(req.downloadHandler.data);
            //        var manifest = manifestAB.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            //        manifestAB.Unload(false);



            //        var allABList = manifest.GetAllAssetBundles();


            //        foreach (var iter in allABList)
            //        {
            //            string oriPath = GameConfigs.GameResExportPath + "/" + iter;
            //            string tarPath = GameConfigs.DownLoadAssetPath + "/" + iter;

            //            req = UnityWebRequest.Get(oriPath);
            //            yield return req.SendWebRequest();

            //            if (req.isNetworkError)
            //            {
            //                Debug.Log("加载文件失败:" + oriPath);
            //            }
            //            else
            //            {
            //                if (File.Exists(tarPath))
            //                {
            //                    File.Delete(tarPath);
            //                }
            //                else
            //                {
            //                    PathUtils.CreateFolderByFilePath(tarPath);
            //                }

            //                Debug.LogFormat("解压缩文件{0}到{1}成功", oriPath, tarPath);


            //                FileStream fs = File.Open(tarPath, FileMode.OpenOrCreate);
            //                fs.Write(req.downloadHandler.data, 0, req.downloadHandler.data.Length);
            //                fs.Flush();
            //                fs.Close();
            //            }
            //        }

            //        Debug.Log("解压缩完成!");
            //    }

            //}
            //else
            //{
            //    Debug.Log("不存在:" + GameConfigs.StreamingAssetManifestPath);
            //}
            #endregion
        }

        /// <summary>
        /// 下载资源
        /// </summary>
        /// <returns></returns>
        private IEnumerator Download()
        {

            var downloadFileList = GetDownloadFileName();
            int totalCount = downloadFileList.Count;
            int count = 0;
            if (totalCount <= 0)
            {
                Debug.Log("没有需要更新的资源");
            }
            else
            {
                foreach (var iter in downloadFileList)
                {
                    string path = string.Empty;
                    if (GameConfigs.developMode == DevelopMode.VirtualLaunch)
                    {
                        path = GameConfigs.OnlineVirtualABRootPath + "/" + iter;
                    }
                    else
                    {
                        path = GameConfigs.OnlineABRootPath + "/" + iter;
                    }

                    UnityWebRequest req = UnityWebRequest.Get(path);
                    yield return req.SendWebRequest();

                    if (req.isNetworkError)
                    {
                        Debug.Log(req.error);
                        yield return null;
                    }
                    else
                    {
                        if (req.responseCode == 200)
                        {
                            byte[] result = req.downloadHandler.data;

                            //save file
                            string downloadPath = GameConfigs.LocalABRootPath + "/" + iter;
                            WriteFile(downloadPath, result);
                            Debug.LogFormat("写入:{0} 成功 -> {1} | len =[{2}]", path, downloadPath, result.Length);

                            AssetBundle onlineManifestAB = AssetBundle.LoadFromMemory(result);
                            onlineManifest = onlineManifestAB.LoadAsset<AssetBundleManifest>("AssetBundlemanifest");
                            onlineManifestAB.Unload(false);
                        }
                    }
                    count++;
                    Debug.LogFormat("下载资源...({0}/{1})", count, totalCount);
                    yield return new WaitForEndOfFrame();
                }
            }
        }

        /// <summary>
        /// 筛选对比需要下载的资源列表
        /// </summary>
        /// <returns></returns>
        private List<string> GetDownloadFileName()
        {
            if (curManifest == null)
            {
                if (onlineManifest == null)
                {
                    return new List<string>();
                }
                else
                {
                    return new List<string>(onlineManifest.GetAllAssetBundles());
                }
            }

            List<string> tempList = new List<string>();
            var curHashCode = curManifest.GetHashCode();
            var onlineHashCode = onlineManifest.GetHashCode();

            if (curHashCode != onlineHashCode)
            {
                // 比对筛选
                var curABList = curManifest.GetAllAssetBundles();
                var onlineABList = onlineManifest.GetAllAssetBundles();
                Dictionary<string, Hash128> curABHashDic = new Dictionary<string, Hash128>();
                foreach (var iter in curABList)
                {
                    curABHashDic.Add(iter, curManifest.GetAssetBundleHash(iter));
                }

                foreach (var iter in onlineABList)
                {
                    if (curABHashDic.ContainsKey(iter))
                    {   //本地有该文件 但与服务器不同
                        Hash128 onlineHash = onlineManifest.GetAssetBundleHash(iter);
                        if (onlineHash != curABHashDic[iter])
                        {
                            tempList.Add(iter);
                        }
                    }
                    else
                    {   //本地没有
                        tempList.Add(iter);
                    }
                }
            }

            return tempList;
        }

        private void WriteFile(string path, byte[] data)
        {
            FileInfo fi = new FileInfo(path);
            DirectoryInfo dir = fi.Directory;
            if (!dir.Exists)
            {
                dir.Create();
            }

            //FileStream fs = fi.Create();
            //fs.Write(data, 0, data.Length);
            //fs.Flush();
            //fs.Close();
            using (FileStream fs = fi.Create())
            {
                fs.Write(data, 0, data.Length);
                fs.Flush();
            }

        }

    }

}