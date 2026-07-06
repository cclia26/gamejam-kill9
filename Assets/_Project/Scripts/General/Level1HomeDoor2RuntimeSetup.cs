using UnityEngine;
using UnityEngine.SceneManagement;

public static class Level1HomeDoor2RuntimeSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ConfigureDoor2ForActiveScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureDoor2ForActiveScene();
    }

    private static void ConfigureDoor2ForActiveScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "Level1_Home")
        {
            return;
        }

        GameObject door2 = GameObject.Find("Door2");
        if (door2 == null)
        {
            return;
        }

        SceneTransition transition = door2.GetComponent<SceneTransition>();
        if (transition == null)
        {
            transition = door2.AddComponent<SceneTransition>();
        }

        transition.Configure("Level1_Rooms", true, true);
        Debug.Log("Level1_Home Door2 runtime transition configured");
    }
}