using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerNameTag : UI_Base
{
    enum Images { Hp, FillImage }

    enum Texts 
    { 
        LevelText, 
        NameText 
    }

    enum GameObjects
    {
        HpBar,
        StaminaBar,
    }

    const float _nameTagHeight = 2.95f;

    GameObject _target;

    static Color _red;
    static Color _green;
    static Color _blue;
    static Color _skyBlue;

    static Color _red_Dark;
    static Color _green_Dark;
    static Color _blue_Dark;
    static Color _skyBlue_Dark;

    private RectTransform _rect;
    private Canvas _canvas;

    public override void Init()
    {
        _rect = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();

        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<GameObject>(typeof(GameObjects));

        ColorUtility.TryParseHtmlString("#D5163A", out _red);
        ColorUtility.TryParseHtmlString("#76CC22", out _green);
        ColorUtility.TryParseHtmlString("#028FEE", out _blue);
        ColorUtility.TryParseHtmlString("#20DFFF", out _skyBlue);

        _red_Dark = _red * 0.5f;
        _green_Dark = _green * 0.5f;
        _blue_Dark = _blue * 0.5f;
        _skyBlue_Dark = _skyBlue * 0.5f;
    }

    private void Awake()
    {
        Init();
    }

    void OnEnable()
    {
        Canvas.willRenderCanvases += UpdatePosition;
    }

    void OnDisable()
    {
        Canvas.willRenderCanvases -= UpdatePosition;
    }

    void Start()
    {

    }

    void Update()
    {

    }

    private void UpdatePosition()
    {
        if (_target == null) return;
        Vector3 worldPos = _target.transform.position + new Vector3(0, _nameTagHeight, 0);
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPos);

        bool isOnScreen = IsOnScreen(screenPoint);

        if (isOnScreen)
        {
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                screenPoint,
                null,
                out localPos
            );

            localPos = ApplyScreenEdgeCorrection(localPos, screenPoint);
            _rect.anchoredPosition = localPos;
        }
        else
        {
            // 화면 밖일 때는 위치를 화면 밖으로 이동
            _rect.anchoredPosition = new Vector2(10000, 10000);
        }
    }

    private Vector2 ApplyScreenEdgeCorrection(Vector2 localPos, Vector3 screenPoint)
    {
        // 화면 중심에서의 거리 계산 (0~1 정규화)
        float screenX = screenPoint.x / Screen.width;
        float screenY = screenPoint.y / Screen.height;

        // 화면 중심 기준으로 오프셋 계산 (0.5가 중심)
        Vector2 centerOffset = new Vector2(screenX - 0.5f, screenY - 0.5f);

        // 좌우/상하 별도 보정 강도 설정
        float horizontalPullStrength = 0.1f;  // 좌우 보정 강도 증가
        float verticalPullStrength = 0.08f;    // 상하 보정 강도

        // 좌우 보정을 위한 추가 계산
        float horizontalEdgeFactor = Mathf.Abs(centerOffset.x) * 2f;  // 0~1
        float verticalEdgeFactor = Mathf.Abs(centerOffset.y) * 2f;    // 0~1

        // 좌우 보정 강화: 가장자리일수록 더 강하게
        horizontalEdgeFactor = Mathf.Pow(horizontalEdgeFactor, 1.3f);  // 곡선적으로 증가
        verticalEdgeFactor = Mathf.Pow(verticalEdgeFactor, 1.2f);      // 상하는 덜 강하게

        // X축: 살짝 안쪽으로 당기기
        float adjustedX = localPos.x - (centerOffset.x * Mathf.Abs(localPos.x) * horizontalPullStrength * horizontalEdgeFactor);

        // Y축: 원래 Y값 유지하거나 약간만 조정
        float adjustedY = localPos.y - (centerOffset.y * Mathf.Abs(localPos.y) * verticalPullStrength * verticalEdgeFactor);

        return new Vector2(adjustedX, adjustedY);
    }

    private bool IsOnScreen(Vector3 screenPoint)
    {
        // 1. 카메라 뒤에 있음
        if (screenPoint.z < 0f) return false;

        // 2. 화면 경계 밖에 있음 (약간의 여유 공간 추가)
        float margin = 100f; // 픽셀 단위 여유공간
        if (screenPoint.x < -margin || screenPoint.x > Screen.width + margin ||
            screenPoint.y < -margin || screenPoint.y > Screen.height + margin)
        {            
            return false;
        }

        return true;
    }

    public void SetLevelText(int level)
    {
        GetText((int)Texts.LevelText).text = level.ToString();
    }

    public void SetNameText(string name, float fontSize)
    {
        var tmp = GetText((int)Texts.NameText);
        if (tmp != null)
        {
            tmp.text = name;
            tmp.fontSize = fontSize;
            tmp.ForceMeshUpdate(); // TMP에 텍스트 즉시 갱신
        }
    }

    public void SetUntargetable()
    {
        SetNameText("대상 지정 불가", 20);
    }

    public void SetUnstoppable()
    {
        SetNameText("이동 방해 면역", 20);
    }

    public void SetTarget(GameObject target)
    {
        _target = target;
    }

    public void SetHPColor(bool darkMode = false)
    {
        if (_target == null)
            return;

        PlayerController targetPc = _target.GetComponent<PlayerController>();
        if (null == targetPc)
            return;

        if (Managers.Object.MyPlayer.gameObject.transform == _target.transform)
            GetImage((int)Images.Hp).color = darkMode ? _green_Dark : _green;
        else if (Managers.Object.MyPlayer.ObjInfo.Player.Team == targetPc.ObjInfo.Player.Team)
            GetImage((int)Images.Hp).color = darkMode ? _blue_Dark: _blue;
        else if (Managers.Object.MyPlayer.ObjInfo.Player.Team != targetPc.ObjInfo.Player.Team)
            GetImage((int)Images.Hp).color = darkMode ? _red_Dark : _red;

        GetImage((int)Images.FillImage).color = darkMode ? _skyBlue_Dark : _skyBlue;
    }

    public void SetHp(float newHp)
    {
        GetObject((int)GameObjects.HpBar).GetComponent<UI_HpBarTick>().SetHp(newHp);
    }
    public void SetMaxHp(float newMaxHp)
    {
        GetObject((int)GameObjects.HpBar).GetComponent<UI_HpBarTick>().SetMaxHp(newMaxHp);
    }
    public void SetBarrier(float newBarrier)
    {
        GetObject((int)GameObjects.HpBar).GetComponent<UI_HpBarTick>().SetBarrier(newBarrier);
    }
    public void SetStamina(float newStamina)
    {
        GetObject((int)GameObjects.StaminaBar).GetComponent<UI_BarNonText>().SetValue(newStamina);
    }
    public void SetMaxStamina(float newMaxStamina)
    {
        GetObject((int)GameObjects.StaminaBar).GetComponent<UI_BarNonText>().SetMaxValue(newMaxStamina);
    }
}
