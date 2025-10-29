using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AbigailCoord : MonoBehaviour
{
    [SerializeField] private Image image;

    GameObject _visionGo;
    VisionCircle _vision;

    int _layer1Team;
    int _layer2Team;

    private void Awake()
    {
        _visionGo = new GameObject();
        _visionGo.name = "VisionCircle";
        _vision = _visionGo.GetOrAddComponent<VisionCircle>();
        _visionGo.transform.SetParent(GetComponentInParent<BaseController>().transform);
        _visionGo.transform.localPosition = Vector3.zero;
        _visionGo.transform.localRotation = Quaternion.identity;
        _visionGo.transform.localScale = Vector3.one;
        string layer1Name = $"FogTeam1";
        string layer2Name = $"FogTeam2";
        _layer1Team = LayerMask.NameToLayer(layer1Name);
        _layer2Team = LayerMask.NameToLayer(layer2Name);

        _vision.SetActivate(false);
    }

    public void ActivateAbigailCoord(float duration, int attackerTeam)
    {
        StartCoroutine(RenderForTime(duration));

        if (attackerTeam == 1)
            _visionGo.layer = _layer1Team;
        else
            _visionGo.layer = _layer2Team;
    }

    public void DeactivateAbigailCoord()
    {
        image.enabled = false;
        _vision.SetActivate(false);
    }

    IEnumerator RenderForTime(float duration)
    {
        image.enabled = true;
        _vision.SetActivate(true);
        yield return new WaitForSeconds(duration);
        image.enabled = false;
        _vision.SetActivate(false);
    }
}
