using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance { get; private set; }

    private string nextScene;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public string GetSceneName(Define.Scene type)
    {
        string name = System.Enum.GetName(typeof(Define.Scene), type);
        return name;
    }

    public void LoadScene(Define.Scene type)
    {
        nextScene = GetSceneName(type);
        SceneManager.LoadScene("Loading");  // 로딩씬으로 이동
    }

    public IEnumerator LoadSceneProcess()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(nextScene);
        op.allowSceneActivation = false;

        float timer = 0f;

        while (!op.isDone)
        {
            if (op.progress < 0.9f)
            {
                // 0 ~ 0.9 구간
                //LoadingUIController.Instance.SetProgress(op.progress);
                Debug.Log("Loading: " + (op.progress * 100f));
            }
            else
            {
                // 0.9 ~ 1.0 구간 (보간)
                timer += Time.deltaTime;
                float progress = Mathf.Lerp(0.9f, 1f, timer);

                //LoadingUIController.Instance.SetProgress(progress);
                Debug.Log("Loading: " + (progress * 100f));
                if (progress >= 1f)
                {
                    op.allowSceneActivation = true;
                }
            }

            yield return null;
        }
    }
}
