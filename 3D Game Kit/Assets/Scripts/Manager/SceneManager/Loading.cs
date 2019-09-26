using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine;
using QFramework;
using UnityEngine.UI;

namespace QF.LSM
{
    public class Loading : MonoBehaviour
    {
        private AsyncOperation async;
        public Slider Slider_Loding;
        public Image Background;
        public Image Logo;
        public Text Text_Loding;

        private IEnumerator Start()
        {
            Slider_Loding.maxValue = 0.9f;
            Slider_Loding.value = 0;
            //异步加载加载3D场景
            async = SceneManager.LoadSceneAsync(LoadSceneManager.Instance.loadSceneName, LoadSceneMode.Additive);
            UIManager.Instance.OpenUI(LoadSceneManager.Instance.loadUIPanel, LoadSceneManager.Instance.uiTargetLevel, LoadSceneManager.Instance.uiData);
            yield return async;
        }

        private void Update()
        {
            if (Slider_Loding.value < async.progress)
            {
                Slider_Loding.value += Time.deltaTime;
            }
            if (Slider_Loding.value == Slider_Loding.maxValue)
            {
                AsyncOperation unloadSceneAsync = SceneManager.UnloadSceneAsync(LoadSceneManager.Instance.loadingSceneName);
                unloadSceneAsync.completed += UnloadSceneAsync_completed;
            }
        }

        private void UnloadSceneAsync_completed(AsyncOperation unloadAsync)
        {
            if (LoadSceneManager.Instance.isStartBattle)
            {
                Client.SpwanerManager.Instance.SendCreatePlayerRequest();
            }
        }
    }
}