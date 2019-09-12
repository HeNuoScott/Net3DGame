using UnityEngine.Networking;
using UnityEngine.Events;
using System.Collections;
using UnityEngine;

namespace QF.UpdateMgr
{
    [MonoSingletonPath("[Framework]/UpdateVersionManager")]
    public class UpdateVersionManager : MonoSingleton<UpdateVersionManager>
    {
        private System.Version curVersion;
        private System.Version onlineVersion;

        public bool IsNeedUpdate;

        /// <summary>
        /// 检查版本是否需要更新
        /// </summary>
        /// <param name="onComplate"></param>
        public void CheckVersion(UnityAction<bool> onComplate = null)
        {
            IsNeedUpdate = false;

            if (GameConfigs.developMode==DevelopMode.EditorDevelop || GameConfigs.developMode == DevelopMode.LocalRelease)
            {
                //如果是开发者模式  直接返回 版本不需要更新
                onComplate?.Invoke(IsNeedUpdate);
            }
            else
            {
                StartCoroutine(Progress(onComplate));
            }
    
        }
        /// <summary>
        /// 版本对比过程
        /// </summary>
        /// <param name="onComplate"></param>
        /// <returns></returns>
        private IEnumerator Progress(UnityAction<bool> onComplate)
        {
            string url = string.Empty;
            if (GameConfigs.developMode == DevelopMode.VirtualLaunch) url = GameConfigs.VirtualServerVersionUrl;
            else url = GameConfigs.ServerVersionUrl;

            //拉取服务器版本
            UnityWebRequest req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();

            if (req.isHttpError || req.isNetworkError)
            {
                Debug.LogError(req.error);
                yield break;
            }

            onlineVersion = new System.Version(req.downloadHandler.text);
            curVersion = new System.Version(Application.version);

            if (onlineVersion != curVersion)
            {
                Debug.LogFormat("当前版本不是最新版本({0}),请及时更新到最新版本({1})", curVersion, onlineVersion);
                IsNeedUpdate = true;
            }

            Debug.Log("版本检测完成!");

            onComplate?.Invoke(IsNeedUpdate);

            yield return null;
        }
    }
}