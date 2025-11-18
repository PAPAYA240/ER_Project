using UnityEngine;

public class LoadingScene : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(LoadingManager.Instance.LoadSceneProcess());
    }
}
