using UnityEngine;

public class LoadingController : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(LoadingManager.Instance.LoadSceneProcess());
    }
}
