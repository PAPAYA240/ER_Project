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

    public void LoadScene(Define.Scene type)
    {
        nextScene = System.Enum.GetName(typeof(Define.Scene), type);

        // 로딩씬을 먼저 열기
        SceneManager.LoadScene("Loading");

        // 다음 프레임에 로딩 프로세스 시작
        StartCoroutine(LoadSceneProcess());
    }

    public IEnumerator LoadSceneProcess()
    {
        yield return null;

        AsyncOperation op = SceneManager.LoadSceneAsync(nextScene);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            yield return null;

            float ProgressValue = Mathf.Clamp01(op.progress / 0.9f);
            LoadingUIController.Instance?.SetProgress(op.progress);

            if (op.progress >= 0.9f)
            {
                LoadingUIController.Instance?.SetProgress(1f);
                op.allowSceneActivation = true;
                yield break;
            }
        }
    }
}
