using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class YukiPyosik : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image image;

    [Header("Sprite Animation Settings")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private bool autoHide = true;

    private GameObject _visionGo;
    private VisionCircle _vision;

    private int _layer1Team;
    private int _layer2Team;

    private Coroutine _animRoutine;

    private void Awake()
    {
        // VisionCircle 생성 및 초기화
        _visionGo = new GameObject("VisionCircle");
        _vision = _visionGo.GetOrAddComponent<VisionCircle>();
        _visionGo.transform.SetParent(GetComponentInParent<BaseController>().transform);
        _visionGo.transform.localPosition = Vector3.zero;
        _visionGo.transform.localRotation = Quaternion.identity;
        _visionGo.transform.localScale = Vector3.one;

        _layer1Team = LayerMask.NameToLayer("FogTeam1");
        _layer2Team = LayerMask.NameToLayer("FogTeam2");

        _vision.SetActivate(false);
        image.enabled = false;

        Texture2D sheet = Resources.Load<Texture2D>("effects/textures/FX_BI_Yuki_01SE");
        if (sheet == null)
        {
            Debug.LogError($"{sheet} not found in Resources");
            return;
        }

        frames = Util.Slice(sheet, 6, 6, 1);
        if (frames == null || frames.Length == 0)
            Debug.LogError("Sprite slicing failed");
    }

    public void ActivateYukiPyosik(int attackerTeam)
    {
        // 팀별로 VisionCircle 레이어 설정
        _visionGo.layer = (attackerTeam == 1) ? _layer1Team : _layer2Team;

        // 중복 재생 방지
        if (_animRoutine != null)
            StopCoroutine(_animRoutine);

        _animRoutine = StartCoroutine(CoPlayAnimation(0.6f));
    }

    private IEnumerator CoPlayAnimation(float duration)
    {
        image.enabled = true;
        _vision.SetActivate(true);

        float frameTime = duration / frames.Length;

        for (int i = 0; i < frames.Length; i++)
        {
            image.sprite = frames[i];
            yield return new WaitForSeconds(frameTime);
        }

        if (autoHide)
        {
            image.enabled = false;
            _vision.SetActivate(false);
        }

        _animRoutine = null;
    }

    public void DeactivateAbigailCoord()
    {
        if (_animRoutine != null)
        {
            StopCoroutine(_animRoutine);
            _animRoutine = null;
        }

        image.enabled = false;
        _vision.SetActivate(false);
    }
}