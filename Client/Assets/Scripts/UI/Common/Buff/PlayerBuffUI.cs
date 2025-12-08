using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static System.Collections.Specialized.BitVector32;

public class PlayerBuffUI : MonoBehaviour
{
    [Header("Buff Icon Prefab")]
    public GameObject buffIconPrefab;

    [Header("Offset Settings")]
    public float rightOffset = -1f;
    public float upOffset = 3.0f;
    public float forwardOffset = 0f;

    private Canvas _worldCanvas;
    private RectTransform _buffPanel;
    private BuffUIFollower _follower;

    readonly private int _maxIconSlot = 2;
    private GameObject[] _icons = new GameObject[2];

    public enum StatusEffect { Heal = 0, Defense = 1, None };

    private void Awake()
    {
        CreateWorldBuffCanvas();
    }

    private void Start()
    {
        AddBuffIcons();
    }

    public void ShowIcon(string effect)
    {
        StatusEffect se = ResolveEffect(effect);
        _icons[(int)se].SetActive(true);
    }

    public void HideIcon(string effect)
    {
        StatusEffect se = ResolveEffect(effect);
        _icons[(int)se].SetActive(false);
    }

    private void CreateWorldBuffCanvas()
    {
        // --- Canvas GameObject 생성 ---
        GameObject canvasGO = new GameObject("WorldBuffCanvas");

        canvasGO.transform.SetParent(null, false);

        // Canvas 컴포넌트
        _worldCanvas = canvasGO.AddComponent<Canvas>();
        _worldCanvas.renderMode = RenderMode.WorldSpace;
        _worldCanvas.worldCamera = Camera.main;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = _worldCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200f, 50f);
        canvasRect.localScale = Vector3.one * 0.005f;

        // --- BuffPanel ---
        GameObject panelGO = new GameObject("BuffPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);

        _buffPanel = panelGO.AddComponent<RectTransform>();
        _buffPanel.anchorMin = new Vector2(0.5f, 0.5f);
        _buffPanel.anchorMax = new Vector2(0.5f, 0.5f);
        _buffPanel.pivot = new Vector2(0.5f, 0.5f);
        _buffPanel.anchoredPosition = Vector2.zero;

        var vLayout = panelGO.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 4f;
        vLayout.childAlignment = TextAnchor.MiddleCenter;
        vLayout.childForceExpandWidth = false;
        vLayout.childForceExpandHeight = false;

        var fitter = panelGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // --- 위치/회전 제어: BuffUIFollower 붙이기 ---
        _follower = canvasGO.AddComponent<BuffUIFollower>();
        _follower.SetTarget(transform);           // 이 스크립트가 붙은 Player
        _follower.rightOffset = rightOffset;
        _follower.upOffset = upOffset;
        _follower.forwardOffset = forwardOffset;
    }

    // 버프 아이콘UI 추가용
    private void AddBuffIcons()
    {
        buffIconPrefab = Managers.Resource.Instantiate("UI/Common/BuffIconUI");
        for (int i = 0; i < _maxIconSlot; i++)
        {
            GameObject icon = Instantiate(buffIconPrefab, _buffPanel);
            icon.SetActive(false);
            _icons[i] = icon;

            BuffIconUI ui = icon.gameObject.GetComponent<BuffIconUI>();
            if (ui != null)
                ui.SetIcon(i);
        }

        //if (buffIconPrefab == null || _buffPanel == null)
        //{
        //    Debug.LogWarning("BuffIconPrefab 또는 BuffPanel이 설정되지 않았습니다.");
        //    return;
        //}
        //
        //GameObject iconGO = Instantiate(buffIconPrefab, _buffPanel);
        //
        //GameObject buffIcon2 = Managers.Resource.Instantiate("UI/Common/BuffIconUI2");
        //Instantiate(buffIcon2, _buffPanel);

        //Image img = iconGO.GetComponent<Image>();
        //if (img != null)
        //    img.sprite = iconSprite;
    }

    private StatusEffect ResolveEffect(string commonName)
    {
        switch (commonName)
        {
            case "Debuff_HealedDecrease":
                return StatusEffect.Heal;
            case "Debuff_Defense":
                return StatusEffect.Defense;
        }

        return StatusEffect.None;
    }
}