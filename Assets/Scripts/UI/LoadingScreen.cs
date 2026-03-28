using System.Collections;
using UnityEngine;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private float minimumDisplayTime = 2f;

    private IEnumerator Start()
    {
        float startTime = Time.realtimeSinceStartup;

        var asyncOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("StartScreen");
        asyncOp.allowSceneActivation = false;

        while (asyncOp.progress < 0.9f)
            yield return null;

        float elapsed = Time.realtimeSinceStartup - startTime;
        if (elapsed < minimumDisplayTime)
            yield return new WaitForSecondsRealtime(minimumDisplayTime - elapsed);

        asyncOp.allowSceneActivation = true;
    }
}
