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
    [SerializeField] private Image emotionImage;
    [SerializeField] private Sprite[] emoticonSprites;

    [Header("Popup Timing")]
    [SerializeField] private float popInDuration = 0.25f;
    [SerializeField] private float keepDuration = /*1.5f;*/ 5f;
    [SerializeField] private float popOutDuration = 0.2f;

    [Header("Scale Curves")]
    [SerializeField]
    private AnimationCurve scaleInCurve = new AnimationCurve(
        new Keyframe(0f, 0.3f),
        new Keyframe(0.4f, 1.2f),
        new Keyframe(1f, 1f)
    );

    [SerializeField]
    private AnimationCurve scaleOutCurve = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(1f, 0f)
    );

    [SerializeField] private float maxScale = 0.5f;

    [Header("Visibility")]
    private CanvasGroup _visibilityGroup;  // 은신/비은신

    private Coroutine _playCo;

    private void Awake()
    {
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);

        _visibilityGroup = GetComponent<CanvasGroup>();
    }

    public void Play(int emoticonIndex)
    {
        if (emoticonIndex >= 0 && emoticonIndex < emoticonSprites.Length)
            emotionImage.sprite = emoticonSprites[emoticonIndex];

        if (_playCo != null)
            StopCoroutine(_playCo);

        gameObject.SetActive(true);
        _playCo = StartCoroutine(CoPlay());
    }

    public void Hide()
    {
        if (_playCo != null)
            StopCoroutine(_playCo);
        _playCo = null;

        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }

    private IEnumerator CoPlay()
    {
        // 1) 팝인
        float t = 0f;
        while (t < popInDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / popInDuration);
            float s = scaleInCurve.Evaluate(normalized) * maxScale;
            transform.localScale = Vector3.one * s;
            yield return null;
        }

        transform.localScale = Vector3.one * maxScale;

        // 2) 유지
        yield return new WaitForSeconds(keepDuration);

        // 3) 팝아웃
        t = 0f;
        while (t < popOutDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / popOutDuration);
            float s = scaleOutCurve.Evaluate(normalized) * maxScale;
            transform.localScale = Vector3.one * s;
            yield return null;
        }

        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
        _playCo = null;
    }

    public void SetVisible(bool visible)
    {
        _visibilityGroup.alpha = visible ? 1f : 0f;
        _visibilityGroup.blocksRaycasts = visible;
    }
}
