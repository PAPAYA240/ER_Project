using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static CombatTextManager;

public class UI_DamageText : UI_Base
{
    #region Member

    CombatTextType _type;
    float _damageValue;
    RectTransform _rectTransform;
    //Poolable _poolable;
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
    public float finalYOffsetFromStart = 0f; // 시작 Y보다 약간 아래로 떨어지게 설정하면 좋다.

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

    #endregion

    public override void Init()
    {
        _rectTransform = gameObject.GetComponent<RectTransform>();
        //_poolable = gameObject.GetOrAddComponent<Poolable>();
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

    }

    public void SetDamageText(CombatTextType type, float value, Vector3 worldPos)
    {
        _type = type;
        _damageValue = value;

        // 초기 위치와 스케일 설정
        _initialPosition = worldPos;
        _initialPosition.x += Random.Range(horizontalDriftRange.x, horizontalDriftRange.y);
        _initialPosition.z += Random.Range(horizontalDriftRange.x, horizontalDriftRange.y);
        transform.localScale = startScale;

        // 투명도 초기화 (이전 사용 후 남아있을 수 있으므로)
        Color currentTextColor = Color.white;
        currentTextColor.a = 1f;

        // 폰트 색 + 숫자 설정.
        switch (_type)
        {
            case CombatTextType.Ad:
                _textMeshProUGUI.text = $"<color=#E00800>{_damageValue.ToString("F0")}</color>"; 
                break;
            case CombatTextType.Ap:
                _textMeshProUGUI.text = $"<color=#DE14D4>{_damageValue.ToString("F0")}</color>";
                break;
            case CombatTextType.True:
                _textMeshProUGUI.text = $"<color=#FFFFFF>{_damageValue.ToString("F0")}</color>";
                break;
            case CombatTextType.HpRecovery:
                _textMeshProUGUI.text = $"<color=#3BE923>{_damageValue.ToString("F0")}</color>";
                break;
            case CombatTextType.StaminaRecovery:
                _textMeshProUGUI.text = $"<color=#2DF0E9>{_damageValue.ToString("F0")}</color>";
                break;
            case CombatTextType.Barrier:
                _textMeshProUGUI.text = $"<color=#E9FF3D>{_damageValue.ToString("F0")}</color>";
                break;
        }


        if(_type == CombatTextType.Ad || _type == CombatTextType.Ap)
        {
            // 최종 도착 지점 계산
            _targetEndPoint = new Vector3(
                _initialPosition.x + Random.Range(horizontalDriftRange.x, horizontalDriftRange.y),
                _initialPosition.y + finalYOffsetFromStart, // 시작 Y좌표에서 최종 Y좌표 오프셋 적용
                _initialPosition.z + Random.Range(horizontalDriftRange.x, horizontalDriftRange.y)
            );
        }
        else if(_type == CombatTextType.Barrier)
        {
            _targetEndPoint = new Vector3(
                _initialPosition.x + Random.Range(horizontalDriftRange.x, horizontalDriftRange.y),
                _initialPosition.y, 
                _initialPosition.z + Random.Range(horizontalDriftRange.x, horizontalDriftRange.y)
            );
        }


        if (null != _coroutine)
            StopCoroutine(_coroutine); // 이전에 실행 중인 코루틴이 있다면 중지

        // 새 애니메이션 코루틴 시작
        switch (type)
        {
            case CombatTextType.Ad:
            case CombatTextType.Ap:
            case CombatTextType.True:
                _coroutine = StartCoroutine(AnimateFloatingText());
                break;
            case CombatTextType.HpRecovery:
            case CombatTextType.StaminaRecovery:
                _coroutine = StartCoroutine(AnimateUpText()); 
                break;
            case CombatTextType.Barrier:
                _coroutine = StartCoroutine(AnimateStayText()); 
                break;
        }
    }

    private IEnumerator AnimateFloatingText()
    {
        float timer = 0f;

        while (timer < totalAnimationDuration)
        {
            timer += Time.deltaTime;
            float t = timer / totalAnimationDuration; // 0에서 1까지 증가하는 비율

            // ====== 위치 계산 (포물선 움직임) ======
            // 시작점과 최종 도착 지점 사이를 선형적으로 보간
            Vector3 currentPosition = Vector3.Lerp(_initialPosition, _targetEndPoint, t);

            // Y축에 포물선 아크 높이를 추가
            // (4 * t * (1 - t))는 t가 0일 때 0, t가 0.5일 때 1, t가 1일 때 0이 되는 포물선 함수
            // 이를 maxUpwardHeight에 곱하면 정점에서 maxUpwardHeight만큼 떠오르게 됨
            float arcHeight = maxUpwardHeight * (4 * t * (1 - t));
            currentPosition.y += arcHeight; // 선형 위치에 포물선 아크 높이를 더해줌

            Vector3 screenPos = Camera.main.WorldToScreenPoint(currentPosition);
            _rectTransform.anchoredPosition = screenPos; // 위치 적용

            // ====== 스케일 계산 (시작 -> 피크 -> 끝) ======
            Vector3 currentScale;
            if (t < 0.5f) // 애니메이션 전반부 (0% ~ 50% 지점): startScale에서 peakScale로 커짐
            {
                currentScale = Vector3.Lerp(startScale, peakScale, t * 2);
            }
            else // 애니메이션 후반부 (50% ~ 100% 지점): peakScale에서 endScale로 작아짐
            {
                currentScale = Vector3.Lerp(peakScale, endScale, (t - 0.5f) * 2);
            }
            transform.localScale = currentScale; // 계산된 스케일 적용

            // ====== 투명도 페이드 아웃 ======
            Color currentColor = _textMeshProUGUI.color;
            currentColor.a = Mathf.Lerp(1f, 0.8f, t); // 0%에서 100%까지 투명도 변화
            _textMeshProUGUI.color = currentColor; // 계산된 투명도 적용

            yield return null; // 다음 프레임까지 대기
        }

        // 애니메이션이 완전히 끝난 후 최종 상태를 명확히 설정
        _textMeshProUGUI.color = new Color(_textMeshProUGUI.color.r, _textMeshProUGUI.color.g, _textMeshProUGUI.color.b, 0f); // 완전히 투명하게
        transform.localScale = endScale; // 최종 스케일 적용

        _coroutine = null;

        // 오브젝트를 풀에 반환
        Managers.Resource.Destroy(gameObject);
    }


    private IEnumerator AnimateUpText()
    {
        float timer = 0f;

        while (timer < totalAnimationDuration)
        {
            timer += Time.deltaTime;
            float t = timer / totalAnimationDuration; // 0에서 1까지 증가하는 비율

            // ====== 위치 계산 ======
            // 시작점과 최종 도착 지점 사이를 선형적으로 보간
            Vector3 currentPosition = Vector3.Lerp(_initialPosition, _initialPosition + new Vector3(0, maxUpwardHeight, 0), t);

            Vector3 screenPos = Camera.main.WorldToScreenPoint(currentPosition);
            _rectTransform.anchoredPosition = screenPos; // 위치 적용

            // ====== 스케일  ======
            Vector3 currentScale;

            currentScale = Vector3.Lerp(peakScale, endScale, t);
            transform.localScale = currentScale; // 스케일 적용

            // ====== 투명도 페이드 아웃 ======
            Color currentColor = _textMeshProUGUI.color;
            currentColor.a = Mathf.Lerp(1f, 0.8f, t); // 0%에서 100%까지 투명도 변화
            _textMeshProUGUI.color = currentColor; // 계산된 투명도 적용

            yield return null; // 다음 프레임까지 대기
        }

        // 애니메이션이 완전히 끝난 후 최종 상태를 명확히 설정
        _textMeshProUGUI.color = new Color(_textMeshProUGUI.color.r, _textMeshProUGUI.color.g, _textMeshProUGUI.color.b, 0f); // 완전히 투명하게
        transform.localScale = endScale; // 최종 스케일 적용

        _coroutine = null;

        // 오브젝트를 풀에 반환
        Managers.Resource.Destroy(gameObject);
    }

    private IEnumerator AnimateStayText()
    {
        float timer = 0f;

        while (timer < totalAnimationDuration)
        {
            timer += Time.deltaTime;
            float t = timer / totalAnimationDuration; // 0에서 1까지 증가하는 비율

            Vector3 screenPos = Camera.main.WorldToScreenPoint(_targetEndPoint);
            _rectTransform.anchoredPosition = screenPos; // 위치 적용

            transform.localScale = Vector3.one; // 스케일 적용

            // ====== 투명도 페이드 아웃 ======
            Color currentColor = _textMeshProUGUI.color;
            currentColor.a = Mathf.Lerp(1f, 0.8f, t); // 0%에서 100%까지 투명도 변화
            _textMeshProUGUI.color = currentColor; // 계산된 투명도 적용

            yield return null; // 다음 프레임까지 대기
        }

        // 애니메이션이 완전히 끝난 후 최종 상태를 명확히 설정
        _textMeshProUGUI.color = new Color(_textMeshProUGUI.color.r, _textMeshProUGUI.color.g, _textMeshProUGUI.color.b, 0f); // 완전히 투명하게
        transform.localScale = endScale; // 최종 스케일 적용

        _coroutine = null;

        // 오브젝트를 풀에 반환
        Managers.Resource.Destroy(gameObject);
    }
}
