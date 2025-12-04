using System;
using System.Collections;
using UnityEngine;

public class UI_TargetingMark : UI_Base
{
    private GameObject _target;
    private Camera _camera;
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private Coroutine _lifetimeCoroutine;
    private Coroutine _updatePositionCoroutine;
    private Action _onComplete;
    private float _duration;

    private Vector3 _offset = new Vector3(0, 2.5f, 0);

    public override void Init()
    {
        _camera = Camera.main;
        if (_camera == null)
            _camera = FindFirstObjectByType<Camera>();

        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
    }

    public void Show(GameObject target, float duration, Action onComplete)
    {
        _target = target;
        _duration = duration;
        _onComplete = onComplete;

        if (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null)
                _camera = FindFirstObjectByType<Camera>();
        }

        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();

        gameObject.SetActive(true);

        // 기존 코루틴들이 있으면 중지
        if (_lifetimeCoroutine != null)
            StopCoroutine(_lifetimeCoroutine);

        if (_updatePositionCoroutine != null)
            StopCoroutine(_updatePositionCoroutine);

        // 위치 업데이트 코루틴 시작
        _updatePositionCoroutine = StartCoroutine(Co_UpdatePosition());

        // 생명주기 코루틴 시작
        _lifetimeCoroutine = StartCoroutine(Co_Lifetime(duration));

        Debug.Log($"Mark Show: duration={duration}초");
    }

    public void Hide()
    {
        Debug.Log($"Mark Hide 호출");

        if (_lifetimeCoroutine != null)
        {
            StopCoroutine(_lifetimeCoroutine);
            _lifetimeCoroutine = null;
        }

        if (_updatePositionCoroutine != null)
        {
            StopCoroutine(_updatePositionCoroutine);
            _updatePositionCoroutine = null;
        }

        gameObject.SetActive(false);

        // Pool 반환은 UIFXManager에서 처리하므로 여기서는 하지 않음
    }

    private IEnumerator Co_UpdatePosition()
    {
        while (gameObject.activeInHierarchy && _target != null)
        {
            Vector3 worldPosition;

            MonsterController mc = _target.GetComponentInChildren<MonsterController>();
            if (mc != null)
                worldPosition = mc.transform.position + _offset;
            else
                worldPosition = _target.transform.position + _offset;

            // Canvas 타입에 따라 좌표 변환
            if (_canvas != null)
            {
                if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    // Screen Space Overlay: 월드 좌표를 스크린 좌표로 변환
                    Vector3 screenPos = _camera.WorldToScreenPoint(worldPosition);
                    _rectTransform.position = screenPos;
                }
                else if (_canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    // Screen Space Camera
                    Vector3 screenPos = _camera.WorldToScreenPoint(worldPosition);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _canvas.transform as RectTransform,
                        screenPos,
                        _canvas.worldCamera,
                        out Vector2 localPos
                    );
                    _rectTransform.localPosition = localPos;
                }
                else
                {
                    // World Space
                    transform.position = worldPosition;
                }
            }
            else
            {
                // Canvas가 없으면 월드 좌표 사용
                transform.position = worldPosition;
            }

            yield return null;  // ← 중요! while 루프 안에 있어야 함
        }
    }

    private IEnumerator Co_Lifetime(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.Log($"Mark Lifetime 끝: {duration}초 경과");

        _onComplete?.Invoke();
        _lifetimeCoroutine = null;

        // 자동으로 Hide 호출
        Hide();
    }
}