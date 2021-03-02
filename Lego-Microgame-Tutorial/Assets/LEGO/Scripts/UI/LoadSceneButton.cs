using Unity.LEGO.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.LEGO.UI
{
    public class LoadSceneButton : MonoBehaviour
    {
        public string sceneName = "";

        public void LoadScene()
        {
            SceneManager.LoadScene(sceneName);
        }

        public void LoadPreviousScene()
        {
            if (GameFlowManager.PreviousScene != null)
            {
                SceneManager.LoadScene(GameFlowManager.PreviousScene);
            }
            else
            {
                LoadScene();
            }
        }
    }
}