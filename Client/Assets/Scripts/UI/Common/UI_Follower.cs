using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class UI_Follower : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform target;            // 따라갈 대상
    public float worldHeight = 1.8f;    // 머리 위 월드 오프셋 (Y)
    public float screenOffset = 105f;   // 화면에서 조금 더 위로 올릴 픽셀

    private RectTransform _rect;
    private Canvas _canvas;
    private Camera _cam;

    private readonly Vector2 _hiddenPos = new Vector2(10000, 10000);

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _cam = Camera.main;
    }

    private void OnEnable()
    {
        Canvas.willRenderCanvases += UpdatePosition;
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= UpdatePosition;

        if (_rect != null)
            _rect.anchoredPosition = _hiddenPos;
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }

    private void UpdatePosition()
    {
        if (_rect == null || _canvas == null || _cam == null || target == null)
        {
            if (_rect != null)
                _rect.anchoredPosition = _hiddenPos;
            return;
        }

        // 1) 월드 기준 머리 위 위치
        Vector3 headWorldPos = target.position + new Vector3(0f, worldHeight, 0f);

        // 2) 스크린 좌표
        Vector3 screenHead = _cam.WorldToScreenPoint(headWorldPos);

        // 카메라 뒤면 숨김
        if (screenHead.z <= 0f)
        {
            _rect.anchoredPosition = _hiddenPos;
            return;
        }

        // 3) 화면에서 조금 위로 올리기
        Vector2 emojiScreenPos = new Vector2(screenHead.x, screenHead.y + screenOffset);

        // 살짝 픽셀 스냅 (이름표와 동일)
        emojiScreenPos = new Vector2(
            Mathf.Round(emojiScreenPos.x * 2f) / 2f,
            Mathf.Round(emojiScreenPos.y * 2f) / 2f
        );

        // 4) 화면 안에 있을 때만 보여주기
        if (!IsOnScreen(emojiScreenPos))
        {
            _rect.anchoredPosition = _hiddenPos;
            return;
        }

        // 5) Canvas 로컬 좌표로 변환해서 anchoredPosition에 적용
        RectTransform canvasRect = _canvas.transform as RectTransform;
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            emojiScreenPos,
            null,                         // Screen Space Overlay
            out localPos
        );

        _rect.anchoredPosition = localPos;
    }

    private bool IsOnScreen(Vector2 screenPoint)
    {
        // 이름표와 동일한 로직
        float margin = 100f;
        if (screenPoint.x < -margin || screenPoint.x > Screen.width + margin ||
            screenPoint.y < -margin || screenPoint.y > Screen.height + margin)
            return false;

        return true;
    }
}
