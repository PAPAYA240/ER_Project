using UnityEngine;

public class Env_HillPack : EnvController

{

    private const float MAX_DIST = 0.3f;



    private float speed = 1.0f;

    private Vector3 _originPosition = Vector3.zero;
    private GameObject _objectSpawner;

    private Renderer _targetRenderer;
    private MaterialPropertyBlock _propertyBlock;
    private int FRESNEL_POWER_ID;

    private const float ACTIVE_FRESNEL = 0.5f;
    private const float UNACTIVE_FRESNEL = 2.0f;
    private const string FRESNEL_POWER_REF = "_FresnelPower";

    protected override void Init()
    {
        base.Init();
        animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        _originPosition = transform.position;

        _targetRenderer = GetComponentInChildren<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();
        FRESNEL_POWER_ID = Shader.PropertyToID(FRESNEL_POWER_REF);

        _isActive = true;
        SetFresnelPower(true);

        GameObject go = Managers.Resource.Instantiate($"Env/ObjectSpawner");
        if (go != null)
        {
            _objectSpawner = go;
            _objectSpawner.transform.position = new Vector3(_originPosition.x, 0.0f, _originPosition.z);
        }
    }

    void Update()
    {
        float newY = Mathf.Sin(Time.time * speed) * MAX_DIST;
        transform.position = _originPosition + new Vector3(0, newY, 0);
        transform.Rotate(0, 50.0f * Time.deltaTime, 0);
    }

    // 활성화 비활성화에 따른 Material 색 변경
    public void SetFresnelPower(bool bActive)
    {
        if (_targetRenderer == null)
            return;

        float fresnelValue = bActive ? ACTIVE_FRESNEL : UNACTIVE_FRESNEL;
        _targetRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(FRESNEL_POWER_ID, fresnelValue);
        _targetRenderer.SetPropertyBlock(_propertyBlock);
    }

    protected override void TryHandleInteraction() 
    {
        _isActive = false;
        //SetFresnelPower(false);
        gameObject.SetActive(false);
    }

}
