using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

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

        // �ε����� ���� ����
        SceneManager.LoadScene("Loading");
    }

    public IEnumerator CoLoadSceneProcess()
    {
        Debug.Log("LoadSceneProcess START");

        yield return null;

        AsyncOperation op = SceneManager.LoadSceneAsync(nextScene);
        op.allowSceneActivation = false;

        float minLoadTime = 1.0f;
        float elapsed = 0f;

        LoadingUIController.Instance?.StartAnimation();

        while (!op.isDone)
        {
            elapsed += Time.deltaTime;

            // ���� progress
            float realProgress = Mathf.Clamp01(op.progress / 0.9f);

            float displayProgress = Mathf.Lerp(0f, 1f, elapsed / minLoadTime);

            LoadingUIController.Instance?.SetProgress(Mathf.Min(realProgress, displayProgress));

            if (displayProgress >= 1f && op.progress >= 0.9f)
            {
                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
