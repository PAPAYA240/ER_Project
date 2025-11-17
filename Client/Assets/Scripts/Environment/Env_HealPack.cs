using UnityEngine;

public class Env_HealPack : EnvController
{
    #region Constants
    // TODO - 나중에 데이터화 해줍시다.
    private const float HEAL_AMOUNT = 650f;


    private const float MAX_DIST = 0.3f;
    private const float ROTATION_SPEED = 50f;
    private const float WAVE_SPEED = 1f;

    private const float ACTIVE_FRESNEL = 2f;
    private const float INACTIVE_FRESNEL = 50f;

    private const string SHADER_FRESNEL_POWER = "_fresnel_power";
    private const string SHADER_OUTLINE_COLOR = "_Color";

    #endregion

    #region Serialized Fields

    [SerializeField] private float _respawnTime = 70f;

    #endregion

    #region Private Fields

    // Transform
    private Vector3 _originPosition;

    // Visual
    private Renderer _targetRenderer;
    private Material _ghostMaterial;
    private Material _outlineMaterial;

    // VFX
    private GameObject _objectSpawner;

    // Timer
    private float _currentTimer = 0f;

    // Colors
    private readonly Color _activeColor = new Color(60f / 255f, 90f / 255f, 52f / 255f, 1f);
    private readonly Color _inactiveColor = new Color(126f / 255f, 114f / 255f, 114f / 255f, 1f);

    // UI
    private UI_HealPack _uiHealPack;
    #endregion

    #region Unity Lifecycle
    protected override void Init()
    {
        base.Init();
        animator = GetComponent<Animator>();

        InitializeTransform();
        InitializeMaterials();
        InitializeVFX();

        GameObject go = Managers.Resource.Instantiate("UI/SubItem/HealPack", gameObject.transform);
        if (go != null)
        {
            _uiHealPack = go.GetComponentInChildren<UI_HealPack>();
            go.transform.localPosition = new Vector3(0, 3.0f, 0);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
        }

        _isActive = true;
        UpdateVisuals(true);
    }

    private void Update()
    {
        UpdateRespawnTimer();
    }
    private void LateUpdate()
    {
        if (_uiHealPack == null || !_uiHealPack.gameObject.activeSelf)
            return;

        // 1. 월드 위치 보정 (힐 팩의 피벗에서 머리 위로 2.0f 정도 올림)
        Vector3 worldPosition = this.transform.position + new Vector3(0, 2.0f, 0);
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

        // 2. RectTransform을 정확히 가져옴
        RectTransform uiRect = _uiHealPack.gameObject.GetComponent<RectTransform>();
        RectTransform parentCanvasRect = uiRect.parent.GetComponent<RectTransform>();

        Vector2 localPoint;

        // 3. Canvas Render Mode에 따라 eventCamera 결정 (오류 방지)
        Canvas rootCanvas = parentCanvasRect.root.GetComponent<Canvas>();
        Camera eventCamera = null;
        if (rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            eventCamera = Camera.main;
        }

        bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvasRect,
            screenPosition,
            eventCamera, 
            out localPoint
        );

        if (converted)
        {
            uiRect.localPosition = localPoint;
        }
    }
    #endregion

    #region Initialization
    private void InitializeTransform()
    {
        _originPosition = transform.position;
    }

    private void InitializeMaterials()
    {
        _targetRenderer = GetComponentInChildren<Renderer>();
        if (_targetRenderer == null)
            return;

        Material[] materials = _targetRenderer.materials;
        _ghostMaterial = materials[0];
        _outlineMaterial = materials[1];
    }

    private void InitializeVFX()
    {
        GameObject spawner = Managers.Resource.Instantiate("Env/ObjectSpawner");
        if (spawner != null)
        {
            _objectSpawner = spawner;
            _objectSpawner.transform.position = new Vector3(_originPosition.x, 0f, _originPosition.z);
        }
    }
    #endregion

    #region Animation
    private void AnimateFloating()
    {
        float newY = Mathf.Sin(Time.time * WAVE_SPEED) * MAX_DIST;
        transform.position = _originPosition + new Vector3(0, newY, 0);
        transform.Rotate(0, ROTATION_SPEED * Time.deltaTime, 0);
    }
    #endregion

    #region Respawn Timer
    private void UpdateRespawnTimer()
    {
        if (_currentTimer <= 0f)
        {
            if (!_isActive)
                Respawn();
            AnimateFloating();
        }
        else
        { 
            _currentTimer -= Time.deltaTime;
        }

        _uiHealPack.SetSecText((int)_currentTimer);
        _uiHealPack.SetProgressAmount(_currentTimer);
    }

    private void Respawn()
    {
        _isActive = true;
        UpdateVisuals(true);
    }

    #endregion

    #region Interaction
    protected override void TryHandleInteraction()
    {
        if (_triggerCreature == null)
            return;

        PlayerController player = _triggerCreature.GetComponent<PlayerController>();
        if (player == null)
            return;

        base.TryHandleInteraction();

        _isActive = false;
        _currentTimer = _respawnTime;

        UpdateVisuals(false);
    }

    #endregion

    #region Visual Updates
    private void UpdateVisuals(bool isActive)
    {
        if (_ghostMaterial == null || _outlineMaterial == null)
            return;

        float fresnelValue = isActive ? ACTIVE_FRESNEL : INACTIVE_FRESNEL;
        Color outlineColor = isActive ? _activeColor : _inactiveColor;

        if (_ghostMaterial.HasProperty(SHADER_FRESNEL_POWER))
        {
            _ghostMaterial.SetFloat(SHADER_FRESNEL_POWER, fresnelValue);
        }

        if (_outlineMaterial.HasProperty(SHADER_OUTLINE_COLOR))
        {
            _outlineMaterial.SetColor(SHADER_OUTLINE_COLOR, outlineColor);
        }
    }

    #endregion
}
