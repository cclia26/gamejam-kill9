using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    [SerializeField] private string desktopSceneName = "Desktop";

    public void StartGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadScene(desktopSceneName);
            return;
        }

        SceneManager.LoadScene(desktopSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
