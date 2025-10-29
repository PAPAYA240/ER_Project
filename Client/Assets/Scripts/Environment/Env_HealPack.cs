using UnityEngine;

public class Env_HealPack : EnvController

{
    #region Constants

    private const float MAX_DIST = 0.3f;
    private const float ROTATION_SPEED = 50f;
    private const float WAVE_SPEED = 1f;

    private const float ACTIVE_FRESNEL = 2f;
    private const float INACTIVE_FRESNEL = 50f;

    private const string SHADER_FRESNEL_POWER = "_fresnel_power";
    private const string SHADER_OUTLINE_COLOR = "_Color";

    #endregion

    #region Serialized Fields

    [SerializeField] private float _respawnTime = 75f;

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

    #endregion

    #region Unity Lifecycle
    protected override void Init()
    {
        base.Init();
        animator = GetComponent<Animator>();

        InitializeTransform();
        InitializeMaterials();
        InitializeVFX();

        _isActive = true;
        UpdateVisuals(true);
    }

    private void Update()
    {
        UpdateRespawnTimer();
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
            _currentTimer -= Time.deltaTime;
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
