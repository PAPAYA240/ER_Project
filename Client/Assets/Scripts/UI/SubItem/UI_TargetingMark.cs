using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class UI_TargetingMark : UI_Base
{
    private RectTransform uiElementRect;

    private GameObject _target;

    private Canvas canvas;
    private float _duration;

    [SerializeField]
    public Vector3 offset = new Vector3(0, 2.5f, 0);

    public override void Init()
    {
        Transform childTransform = transform.Find("Image"); 
        if (childTransform != null)
            uiElementRect = childTransform.GetComponent<RectTransform>();

        uiElementRect.gameObject.SetActive(false);
        mainCamera = Camera.main;
    }

    public void SetTarget(GameObject target, float duration = 10.0f)
    {
        _target = target;
        _duration = duration;
        _elapsedTime = 0;
        StartCoroutine(CoActive());
    }

    private Camera mainCamera;
    float _elapsedTime = 0;
    IEnumerator CoActive()
    {
        while (_elapsedTime < _duration)
        {
            _elapsedTime += Time.deltaTime;
            if (_target == null || uiElementRect == null || mainCamera == null)
                yield break;

            Vector3 worldPosition = _target.transform.position + offset;
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

            if (screenPosition.z < 0)
            {
                uiElementRect.gameObject.SetActive(false);
                yield break;
            }

            Vector2 localPoint;
            RectTransform parentRect = uiElementRect.parent as RectTransform; // 부모 RectTransform 캐싱

            if (parentRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPosition,
                null,
                out localPoint))
            {
                uiElementRect.localPosition = localPoint;
            }

            uiElementRect.gameObject.SetActive(true);
            yield return null;
        }
        // 스턴 끝
        _target.GetComponent<CreatureController>().IsStun = false;
        uiElementRect.gameObject.SetActive(false);
    }
}
