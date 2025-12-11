using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Device;
using static UI_Minimap;
using static UnityEngine.GraphicsBuffer;

public class UI_MonsterHpBar : UI_Base
{
    enum Texts 
    { 
        LevelText, 
        HpText 
    }

    enum GameObjects 
    {
        HpBar,
        Patience,
        TextBg
    }

    public float NameTagHeight { get; set; } = 1.9f;

    GameObject _target;
    private RectTransform _rect;
    private Canvas _canvas;
    private MonsterController _mc;

    public override void Init()
    {
        _rect = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();

        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<GameObject>(typeof(GameObjects));
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

    void OnEnable()
    {
        Canvas.willRenderCanvases += UpdatePosition;
    }

    void OnDisable()
    {
        _rect.anchoredPosition = new Vector2(10000, 10000);
        Canvas.willRenderCanvases -= UpdatePosition;
    }

    //private void LateUpdate()
    //{
    //    if (_target != null)
    //    {
    //        UpdatePosition();
    //    }
    //}

    private void UpdatePosition()
    {
        if (_target == null || _mc == null || _mc.State == CreatureState.Dead)
        {
            _rect.anchoredPosition = new Vector2(10000, 10000);
            return;
        }

        Vector3 headWorldPos = _target.transform.position + new Vector3(0, NameTagHeight, 0);
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
        //Vector3 worldPos = _target.transform.position + new Vector3(0, NameTagHeight, 0);
        //Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        //gameObject.transform.position = screenPos;
    }

    private bool IsOnScreen(Vector3 screenPoint)
    {
        // 1. 카메라 뒤에 있음
        if (screenPoint.z < 0f) return false;

        // 2. 화면 경계 밖에 있음 (약간의 여유 공간 추가)
        float margin = 100f; // 픽셀 단위 여유공간
        if (screenPoint.x < -margin || screenPoint.x > UnityEngine.Screen.width + margin ||
            screenPoint.y < -margin || screenPoint.y > UnityEngine.Screen.height + margin)
        {
            return false;
        }

        return true;
    }

    public void SetLevelText(int level)
    {
        GetText((int)Texts.LevelText).text = level.ToString();
    }

    public void SetHpText(string str)
    {
        GetText((int)Texts.HpText).text = str;
    }

    public void SetTarget(GameObject target)
    {
        _target = target;
        MonsterController targetMc = _target.GetComponentInChildren<MonsterController>();
        if (null == targetMc)
            return;

        switch (targetMc.ObjInfo.Monster.MonsterType)
        {
            case MonsterType.Drone:
                NameTagHeight = 1.2f;
                break;
            case MonsterType.Omega:
                NameTagHeight = 1.8f;
                break;
            case MonsterType.Gamma:
                NameTagHeight = 2.7f;
                break;
        }

        _mc = targetMc;
    }

    public void SetHp(float newHp)
    {
        GetObject((int)GameObjects.HpBar).GetComponent<UI_BarTick>().SetValue(newHp);
    }
    public void SetStamina(float newStamina)
    {
        GetObject((int)GameObjects.Patience).GetComponent<UI_BarNonText>().SetValue(newStamina);
    }
}
