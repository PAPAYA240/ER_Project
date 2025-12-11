using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Emoticon : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] Image emotionImage;    // 표시할 이미지
    [SerializeField] Sprite[] charSprites;  // 캐릭터 별 스프라이트

    [Header("Target")]
    public GameObject target;               // 따라갈 대상
    public float worldHeight = 1.8f;        // 머리 위 높이
    public float screenOffset = 80f;        // 화면에서 살짝 더 위로 띄우는 픽셀값

    private RectTransform _rect;
    private Canvas _canvas;

    [Header("Popup Timing")]
    [SerializeField] private float popInDuration = 0.25f;   // 등장(커지는) 시간
    [SerializeField] private float keepDuration = 1.5f;     // 최종 크기로 유지 시간
    [SerializeField] private float popOutDuration = 0.2f;   // 사라질 때(작아지는) 시간

    [Header("Scale Curves")]
    // 등장할 때 스케일 곡선 (0~1 구간)
    [SerializeField]
    private AnimationCurve scaleInCurve =
        new AnimationCurve(
            new Keyframe(0f, 0.3f),
            new Keyframe(0.4f, 1.2f),
            new Keyframe(1f, 1f)
        );

    // 사라질 때 스케일 곡선 (0~1 구간, 1 -> 0으로 줄어드는 느낌)
    [SerializeField]
    private AnimationCurve scaleOutCurve =
        new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0f)
        );
    [SerializeField] private float maxScale = 0.6f;   // 최대 스케일

    private Coroutine _playCo;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        Canvas.willRenderCanvases += UpdatePosition;

        // 여기서부터 애니메이션 시작
        if (_playCo != null)
        {
            StopCoroutine(_playCo);
            _playCo = null;
        }

        transform.localScale = Vector3.zero;
        _playCo = StartCoroutine(CoPlay());
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= UpdatePosition;

        if (_playCo != null)
        {
            StopCoroutine(_playCo);
            _playCo = null;
        }

        // 멀리 숨기기
        if (_rect != null)
            _rect.anchoredPosition = new Vector2(10000, 10000);
    }

    public void SetTarget(GameObject t)
    {
        target = t;
    }

    public void Play(int emoticonIndex)
    {
        emotionImage.sprite = charSprites[emoticonIndex];
        // 기본은 꺼져있다가, 호출 시 켜지면서 OnEnable → CoPlay() 자동 실행
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        else
        {
            // 이미 떠 있는데 또 누르면 애니메이션 리셋하고 싶다면:
            gameObject.SetActive(false);
            gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private IEnumerator CoPlay()
    {
        // 1) 등장 (scaleInCurve)
        float t = 0f;
        while (t < popInDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / popInDuration);
            float scale = scaleInCurve.Evaluate(normalized);
            transform.localScale = Vector3.one * (scale * maxScale);
            yield return null;
        }

        transform.localScale = Vector3.one * scaleInCurve.Evaluate(1f) * maxScale;

        // 2) 유지
        yield return new WaitForSeconds(keepDuration);

        // 3) 축소 (scaleOutCurve)
        t = 0f;
        while (t < popOutDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / popOutDuration);
            float scale = scaleOutCurve.Evaluate(normalized);
            transform.localScale = Vector3.one * (scale * maxScale);
            yield return null;
        }

        transform.localScale = Vector3.zero;

        // 4) 비활성화 (다음 키 입력까지 숨김)
        gameObject.SetActive(false);
        _playCo = null;
    }

    private void UpdatePosition()
    {
        if (target == null)
        {
            _rect.anchoredPosition = new Vector2(10000, 10000);
            return;
        }

        // 1) 월드 기준 머리 위 위치
        Vector3 headWorldPos = target.transform.position + new Vector3(0f, worldHeight, 0f);

        // 2) 스크린 좌표로 변환
        Vector3 screenHead = Camera.main.WorldToScreenPoint(headWorldPos);

        // 카메라 뒤에 있으면 숨김
        if (screenHead.z <= 0)
        {
            _rect.anchoredPosition = new Vector2(10000, 10000);
            return;
        }

        // 3) 화면에서 조금 더 위로 오프셋
        Vector2 emoticonScreenPos = new Vector2(screenHead.x, screenHead.y) + Vector2.up * screenOffset;

        // 4) 약간의 픽셀 스냅 (네임태그와 동일)
        emoticonScreenPos = new Vector2(
            Mathf.Round(emoticonScreenPos.x * 2f) / 2f,
            Mathf.Round(emoticonScreenPos.y * 2f) / 2f
        );

        // 5) 화면 안/밖 체크 (원하면 네임태그 IsOnScreen 그대로 써도 됨)
        if (!IsOnScreen(emoticonScreenPos))
        {
            _rect.anchoredPosition = new Vector2(10000, 10000);
            return;
        }

        // 6) Canvas 로컬 좌표로 변환 후 anchoredPosition 세팅
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            emoticonScreenPos,
            null,        // Screen Space - Overlay라서 null
            out localPos
        );

        _rect.anchoredPosition = localPos;
    }

    private bool IsOnScreen(Vector2 screenPoint)
    {
        float margin = 100f;
        if (screenPoint.x < -margin || screenPoint.x > Screen.width + margin ||
            screenPoint.y < -margin || screenPoint.y > Screen.height + margin)
        {
            return false;
        }

        return true;
    }
}
