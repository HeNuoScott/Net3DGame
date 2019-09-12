using UnityEngine;
using QFramework;

namespace QF.UpdateMgr
{

    /// <summary>
    /// 启动案例
    /// </summary>
    public class Launcher : MonoBehaviour
    {
        public DevelopMode mode = DevelopMode.EditorDevelop;
        

        private void Start()
        {
            Debug.Log(new System.Version(Application.version));
            GameConfigs.developMode = mode;

            QF.Res.ResKit.Init();

            QFramework.UIMgr.SetResolution(1920, 1080, 0);

            UpdateVersionManager.Instance.CheckVersion((bool needUpdate) =>
            {
                if (needUpdate) //版本需要更新
                {
                    Debug.Log("版本需要更新,在这里执行大版本更新");
                }
                else  //版本不需要更新  检测资源是否需要更新需要更新则更新  不需要更新则开始游戏
                {
                    UpdateAssetManager.Instance.CheckAsset(() => 
                    {
                        Debug.Log("开始游戏");
                        LSM.LoadSceneManager.Instance.LoadSceneAsync("LobbyPanel", UILevel.Common);
                    });
                }
            });
        }
        

    }
}