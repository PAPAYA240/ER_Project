using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static CombatTextManager;

public class UI_DamageText : UI_Base
{
    TextType _type;
    float _damageValue;
    RectTransform _rectTransform;
    Poolable _poolable;
    TextMeshProUGUI _textMeshProUGUI;

    // ====== 설정 가능한 애니메이션 변수들 ======
    [Header("Animation Settings")]
    [Tooltip("애니메이션 전체 지속 시간 (올라갔다가 떨어지는 과정 포함)")]
    public float totalAnimationDuration = 0.5f; // 전체 애니메이션 시간
    [Tooltip("시작점에서 가장 높은 지점까지 올라가는 높이 (포물선 아크의 높이)")]
    public float maxUpwardHeight = 0.2f;
    [Tooltip("좌우로 움직이는 범위 (X축 기준)")]
    public Vector2 horizontalDriftRange = new Vector2(-1f, 1f);
    [Tooltip("최종적으로 시작점 Y좌표로부터 얼마나 떨어질지 (음수 값도 가능)")]
    public float finalYOffsetFromStart = 0f; // 시작 Y보다 약간 아래로 떨어지게 설정하면 좋습니다.

    [Tooltip("애니메이션 시작 시 텍스트의 크기")]
    public Vector3 startScale = Vector3.one * 0.8f;
    [Tooltip("애니메이션 중 텍스트의 최대 크기 (포물선의 정점 부근)")]
    public Vector3 peakScale = Vector3.one * 1.2f;
    [Tooltip("애니메이션 종료 시 텍스트의 크기")]
    public Vector3 endScale = Vector3.one * 0.8f;

    // ====== 내부 애니메이션 관련 변수 ======
    private Vector3 _initialPosition; // 데미지 텍스트가 시작되는 월드 좌표
    private Vector3 _targetEndPoint;  // 포물선이 끝나는 최종 월드 좌표

    Coroutine _coroutine = null;


    public override void Init()
    {
        _rectTransform = gameObject.GetComponent<RectTransform>();
        _poolable = gameObject.GetOrAddComponent<Poolable>();
        _textMeshProUGUI = gameObject.GetComponent<TextMeshProUGUI>();
    }

    private void Awake()
    {
        Init();
    }

    void Start()
    {
        
    }

    void Update()
    {
        //if (_rectTransform != null && _poolable != null && _poolable.IsUsing == true)
        //{
        //    //UpdateAnim();
        //}
    }

    public void SetDamageText(TextType type, float value, Vector3 worldPos)
    {
        _type = type;
        _damageValue = value;

        // 초기 위치와 스케일 설정
        _initialPosition = worldPos;
        transform.localScale = startScale;

        // 투명도 초기화 (이전 사용 후 남아있을 수 있으므로)
        Color currentTextColor = Color.white;
        currentTextColor.a = 1f;

        //폰트 색 + 숫자 설정.
        switch (type)
        {
            case TextType.AdDamage:
                _textMeshProUGUI.text = $"<color=#E00800>{_damageValue.ToString("F0")}</color>"; 
                break;
            case TextType.ApDamage:
                _textMeshProUGUI.text = $"<color=#DE14D4>{_damageValue.ToString("F0")}</color>";
                break;
            case TextType.TrueDamage:
                _textMeshProUGUI.text = $"<color=#FFFFFF>{_damageValue.ToString("F0")}</color>";
                break;
            case TextType.HpRecovery:
                _textMeshProUGUI.text = $"<color=#3BE923>{_damageValue.ToString("F0")}</color>";
                break;
            case TextType.StaminaRecovery:
                _textMeshProUGUI.text = $"<color=#2DF0E9>{_damageValue.ToString("F0")}</color>";
                break;
            case TextType.Barrier:
                _textMeshProUGUI.text = $"<color=#83817D>{_damageValue.ToString("F0")}</color>";
                break;
        }

        //최종 도착 지점 계산: 시작점에서 좌우로 랜덤하게 움직이고, Y축으로 finalYOffsetFromStart만큼 이동합니다.
        _targetEndPoint = new Vector3(
            _initialPosition.x + Random.Range(horizontalDriftRange.x, horizontalDriftRange.y),
            _initialPosition.y + finalYOffsetFromStart, // 시작 Y좌표에서 최종 Y좌표 오프셋 적용
            _initialPosition.z + Random.Range(horizontalDriftRange.x, horizontalDriftRange.y)
        );

        if(null != _coroutine)
            StopCoroutine(_coroutine); // 이전에 실행 중인 코루틴이 있다면 중지
        _coroutine = StartCoroutine(AnimateFloatingText()); // 새 애니메이션 코루틴 시작
    }

    private IEnumerator AnimateFloatingText()
    {
        float timer = 0f;

        while (timer < totalAnimationDuration)
        {
            timer += Time.deltaTime;
            float t = timer / totalAnimationDuration; // 0에서 1까지 증가하는 비율

            Debug.Log($"{timer}{totalAnimationDuration}");

            // ====== 위치 계산 (포물선 움직임) ======
            // 시작점과 최종 도착 지점 사이를 선형적으로 보간합니다.
            Vector3 currentPosition = Vector3.Lerp(_initialPosition, _targetEndPoint, t);

            // Y축에 포물선 아크 높이를 추가합니다.
            // (4 * t * (1 - t))는 t가 0일 때 0, t가 0.5일 때 1, t가 1일 때 0이 되는 포물선 함수입니다.
            // 이를 maxUpwardHeight에 곱하면 정점에서 maxUpwardHeight만큼 떠오르게 됩니다.
            float arcHeight = maxUpwardHeight * (4 * t * (1 - t));
            currentPosition.y += arcHeight; // 선형 위치에 포물선 아크 높이를 더해줍니다.

            Vector3 screenPos = Camera.main.WorldToScreenPoint(currentPosition);
            _rectTransform.anchoredPosition = screenPos;
            //Vector3 screenPos = Camera.main.WorldToScreenPoint(currentPosition);
            //if(RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, new Vector2(screenPos.x, screenPos.y), Camera.main, out Vector2 local))
            //{
            //    _rectTransform.localPosition = local; // 계산된 위치 적용
            //}


            // ====== 스케일 계산 (시작 -> 피크 -> 끝) ======
            Vector3 currentScale;
            if (t < 0.5f) // 애니메이션 전반부 (0% ~ 50% 지점): startScale에서 peakScale로 커집니다.
            {
                currentScale = Vector3.Lerp(startScale, peakScale, t * 2);
            }
            else // 애니메이션 후반부 (50% ~ 100% 지점): peakScale에서 endScale로 작아집니다.
            {
                currentScale = Vector3.Lerp(peakScale, endScale, (t - 0.5f) * 2);
            }
            transform.localScale = currentScale; // 계산된 스케일 적용

            // ====== 투명도 페이드 아웃 ======
            Color currentColor = _textMeshProUGUI.color;
            currentColor.a = Mathf.Lerp(1f, 0.8f, t); // 0%에서 100%까지 투명도를 1에서 0으로 변화
            _textMeshProUGUI.color = currentColor; // 계산된 투명도 적용

            yield return null; // 다음 프레임까지 대기
        }

        // 애니메이션이 완전히 끝난 후 최종 상태를 명확히 설정합니다.
        _textMeshProUGUI.color = new Color(_textMeshProUGUI.color.r, _textMeshProUGUI.color.g, _textMeshProUGUI.color.b, 0f); // 완전히 투명하게
        transform.localScale = endScale; // 최종 스케일 적용

        _coroutine = null;

        // 오브젝트를 풀에 반환
        Managers.Resource.Destroy(gameObject);
    }

    //void UpdateAnim()
    //{
    //    // 어떻게 움직일 지

    //    Vector3 screenPos = Camera.main.WorldToScreenPoint(_worldPos);

    //    _curTime = Mathf.Min(_curTime + Time.deltaTime, _maxTime);

    //    float ratio = _curTime / _maxTime;

    //    switch (_type)
    //    {
    //        case TextType.AdDamage:
    //        case TextType.ApDamage:
    //        case TextType.TrueDamage:
    //            {


    //            }
    //            break;
    //        case TextType.HpRecovery:
    //        case TextType.StaminaRecovery:
    //            {

    //            }
    //            break;
    //        case TextType.Barrier:

    //            break;
    //    }
    //}
}
