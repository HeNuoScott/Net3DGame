using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QF.LSM
{
    [MonoSingletonPath("[Framework]/LoadSceneManager")]
    public class LoadSceneManager : MonoSingleton<LoadSceneManager>
    {
        private AsyncOperation async;

        public string loadSceneName;
        public string loadUIPanel;
        public UILevel uiTargetLevel;
        public IUIData uiData;
        public string defaultScene = "Lobby";
        public string loadingSceneName = "Loading";

        /// <summary>
        /// ���س���(�����л�,�Դ���������)
        /// </summary>
        /// <param name="loadingData"></param>
        /// <returns></returns>
        private IEnumerator Load(string loadingScene)
        {
            async = SceneManager.LoadSceneAsync(loadingScene);
            yield return async;
            UIMgr.CloseAllPanel();
        }

        /// <summary>
        /// ����Ĭ�ϳ���
        /// Ĭ�ϻ����֮ǰUIRoot��������
        /// </summary>
        /// <param name="uiPanel">����UI</param>
        /// <param name="uiLevel">UI�㼶</param>
        public void LoadSceneAsync(string uiPanel, UILevel uiLevel, IUIData uiData = null)
        {
            loadSceneName = defaultScene;
            loadingSceneName = "Loading";
            loadUIPanel = uiPanel;
            uiTargetLevel = uiLevel;
            this.uiData = uiData;
            StartCoroutine(Load(loadingSceneName));
        }
        /// <summary>
        /// ���س���
        /// Ĭ�ϻ����֮ǰUIRoot��������
        /// </summary>
        /// <param name="scene">Ŀ�곡��</param>
        /// <param name="uiPanel">����UI</param>
        /// <param name="uiLevel">UI�㼶</param>
        public void LoadSceneAsync(string scene, string uiPanel, UILevel uiLevel, IUIData uiData = null)
        {
            loadSceneName = scene;
            loadingSceneName = "Loading";
            loadUIPanel = uiPanel;
            uiTargetLevel = uiLevel;
            this.uiData = uiData;
            StartCoroutine(Load(loadingSceneName));
        }
        /// <summary>
        /// ͨ���Զ�����ȳ��� ����Ĭ�ϳ���
        /// Ĭ�ϻ����֮ǰUIRoot��������
        /// </summary>
        /// <param name="loadingScene">���ȳ���</param>
        /// <param name="uiPanel">����UI</param>
        /// <param name="uiLevel">UI�㼶</param>
        public void CustomLoadSceneAsync(string loadingScene,string uiPanel, UILevel uiLevel, IUIData uiData = null)
        {
            loadSceneName = defaultScene;
            loadingSceneName = loadingScene;
            loadUIPanel = uiPanel;
            uiTargetLevel = uiLevel;
            this.uiData = uiData;
            StartCoroutine(Load(loadingSceneName));
        }
        /// <summary>
        /// ͨ���Զ�����ȳ��� ���س���
        /// Ĭ�ϻ����֮ǰUIRoot��������
        /// </summary>
        /// <param name="loadingScene">���ȳ���</param>
        /// <param name="scene">Ŀ�곡��</param>
        /// <param name="uiPanel">����UI</param>
        /// <param name="uiLevel">UI�㼶</param>
        public void CustomLoadSceneAsync(string loadingScene, string scene, string uiPanel, UILevel uiLevel, IUIData uiData = null)
        {
            loadSceneName = scene;
            loadingSceneName = loadingScene;
            loadUIPanel = uiPanel;
            uiTargetLevel = uiLevel;
            this.uiData = uiData;
            StartCoroutine(Load(loadingSceneName));
        }
    }
}