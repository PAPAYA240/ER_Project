using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class YukiPyosik : MonoBehaviour
{
    [SerializeField] private Image image;

    [SerializeField] private Sprite[] _frames;
    [SerializeField] private bool autoHide = true;

    [SerializeField] private GameObject _target;
    [SerializeField] private Camera mainCamera;

    private Coroutine _coAnimRoutine;

    private void Awake()
    {
        image = GetComponent<Image>();
        if (mainCamera == null)
            mainCamera = Camera.main;

        Texture2D sheet = Resources.Load<Texture2D>("effects/textures/FX_BI_Yuki_01SE");
        if (sheet == null)
        {
            //Debug.LogError($"Not found in Resources");
            return;
        }

        _frames = Util.Slice(sheet, 6, 6, 1);
        if (_frames == null || _frames.Length == 0)
        {
            //Debug.LogError("Sprite slicing failed");
        }            
    }

    private void LateUpdate()
    {
        if (_target == null)
            return;

        // 월드 좌표에서 스크린 좌표
        Vector3 screenPos = mainCamera.WorldToScreenPoint(_target.transform.position) + new Vector3(0f, 40f, 0f);

        transform.position = screenPos;
    }

    public void ActivateYukiPyosik(GameObject go)
    {
        _target = go;

        // 중복 재생 방지
        if (_coAnimRoutine != null)
            StopCoroutine(_coAnimRoutine);

        _coAnimRoutine = StartCoroutine(CoPlayAnimation(0.6f));
    }

    private IEnumerator CoPlayAnimation(float duration)
    {
        image.enabled = true;

        float frameTime = duration / _frames.Length;

        for (int i = 0; i < _frames.Length; i++)
        {
            image.sprite = _frames[i];
            yield return new WaitForSeconds(frameTime);
        }

        if (autoHide)
            image.enabled = false;

        _coAnimRoutine = null;
    }
}