using Google.Protobuf.Protocol;
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

    const float _nameTagHeight = 1.8f;

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
    private PlayerController _pc;

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
        _rect.anchoredPosition = new Vector2(10000, 10000);
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
        if (_target == null || _pc == null || _pc.State == CreatureState.Dead)
        {
            _rect.anchoredPosition = new Vector2(10000, 10000);
            return;
        }

        Vector3 headWorldPos = _target.transform.position + new Vector3(0, _nameTagHeight, 0);
        Vector3 screenHead = Camera.main.WorldToScreenPoint(headWorldPos);

        if (screenHead.z <= 0)
        {
            _rect.anchoredPosition = new Vector2(10000, 10000);
            return;
        }

        Vector2 screenDirection = Vector2.up;

        float screenOffset = 40f;
        Vector2 nameTagScreenPos = new Vector2(screenHead.x, screenHead.y) + (screenDirection * screenOffset);

        nameTagScreenPos = new Vector2(
            Mathf.Round(nameTagScreenPos.x * 2f) / 2f,
            Mathf.Round(nameTagScreenPos.y * 2f) / 2f
        );

        if (IsOnScreen(nameTagScreenPos))
        {
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                nameTagScreenPos,
                null,
                out localPos
            );

            _rect.anchoredPosition = localPos;
        }
        else
        {
            _rect.anchoredPosition = new Vector2(10000, 10000);
        }
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
        PlayerController targetPc = _target.GetComponent<PlayerController>();
        if (null == targetPc)
            return;
        _pc = targetPc;
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

    public void SetVisible(bool visible)
    {
        // UI는 CanvasRenderer로 충분
        CanvasRenderer[] canvasRenderers = GetComponentsInChildren<CanvasRenderer>(true);
        foreach (var cr in canvasRenderers)
        {
            cr.SetAlpha(visible ? 1f : 0f);
        }
    }
}
