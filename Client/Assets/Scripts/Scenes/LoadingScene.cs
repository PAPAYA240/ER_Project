using System.Collections;
using UnityEngine;

public class LoadingScene : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(StartLoadProcess());
    }

    private IEnumerator StartLoadProcess()
    {
        // Loading 씬이 완전히 초기화된 뒤 시작
        yield return null;

        yield return StartCoroutine(LoadingManager.Instance.CoLoadSceneProcess());
    }
}
