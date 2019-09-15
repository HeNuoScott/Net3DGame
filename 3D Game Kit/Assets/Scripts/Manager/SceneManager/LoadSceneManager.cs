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
        /// 加载场景(场景切换,自带场景过度)
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
        /// 加载默认场景
        /// 默认会清除之前UIRoot所有内容
        /// </summary>
        /// <param name="uiPanel">场景UI</param>
        /// <param name="uiLevel">UI层级</param>
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
        /// 加载场景
        /// 默认会清除之前UIRoot所有内容
        /// </summary>
        /// <param name="scene">目标场景</param>
        /// <param name="uiPanel">场景UI</param>
        /// <param name="uiLevel">UI层级</param>
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
        /// 通过自定义过度场景 加载默认场景
        /// 默认会清除之前UIRoot所有内容
        /// </summary>
        /// <param name="loadingScene">过度场景</param>
        /// <param name="uiPanel">场景UI</param>
        /// <param name="uiLevel">UI层级</param>
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
        /// 通过自定义过度场景 加载场景
        /// 默认会清除之前UIRoot所有内容
        /// </summary>
        /// <param name="loadingScene">过度场景</param>
        /// <param name="scene">目标场景</param>
        /// <param name="uiPanel">场景UI</param>
        /// <param name="uiLevel">UI层级</param>
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