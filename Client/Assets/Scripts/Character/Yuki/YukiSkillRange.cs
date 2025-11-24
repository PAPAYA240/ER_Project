using UnityEngine;

public class YukiSkillRange : MonoBehaviour
{
    public GameObject _rangePrefab;

    private void Awake()
    {
        _rangePrefab = Managers.Resource.Instantiate("Effect/Yuki_R");
    }

    public void SetPosition(Vector3 position)
    {
        _rangePrefab.transform.position = position;
    }

    public void HideSkillRange()
    {
        if (_rangePrefab != null)
            Destroy(_rangePrefab);
    }
}
