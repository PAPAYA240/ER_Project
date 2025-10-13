using UnityEngine;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    Define.CameraMode _mode = Define.CameraMode.QuaterView;

    Vector3 _farDelta = new Vector3(-4.0f, 14.0f, 5.0f);
    Vector3 _nearDelta = new Vector3(-4.0f, 6.0f, 5.0f);
    Vector3 _delta;
    float _lastZoom = 0f;
    float[] _zoomSteps = { 10f, 8f, 5f, 4f };
    int _currentStep = 0;
    float _currentZoom = 0f;
    float _targetZoom = 0f;
    bool _isLerpComplete = true;
    float _zoomSpeed = 0f;
    float _lerpSpeed = 0f;

    [SerializeField]
    GameObject _player = null;
    private Camera _mainCamera;
    private Camera _mapCamera;
    private Camera _uiCamera;

    public Action LateUpdateAction = null;

    public void SetPlayer(GameObject player) { _player = player; }

    void Start()
    {
        _mainCamera = Camera.main;

        if (GetComponent<PhysicsRaycaster>() == null)
            gameObject.AddComponent<PhysicsRaycaster>();

        SetupLayerCameras_URP(); 

        _currentZoom = _zoomSteps[_currentStep];
        _targetZoom = _currentZoom;
        _lastZoom = _zoomSteps[_zoomSteps.Length - 1];
        _zoomSpeed = 8f;
        _lerpSpeed = 16f;
    }

    void SetupLayerCameras_URP()
    {
        var mainCamData = _mainCamera.gameObject.GetOrAddComponent<UniversalAdditionalCameraData>();
        mainCamData.renderType = CameraRenderType.Base;
        mainCamData.cameraStack.Clear();
        _mainCamera.clearFlags = CameraClearFlags.SolidColor; 
        _mainCamera.cullingMask = (1 << LayerMask.NameToLayer("Map"));

        GameObject uiCamObj = new GameObject("UICamera");
        uiCamObj.transform.SetParent(this.transform);
        _uiCamera = uiCamObj.AddComponent<Camera>();
        _uiCamera.CopyFrom(_mainCamera);
        _uiCamera.clearFlags = CameraClearFlags.Nothing;
        _uiCamera.cullingMask = (1 << LayerMask.NameToLayer("IndicatorUI"));

        var uiCamData = _uiCamera.gameObject.GetOrAddComponent<UniversalAdditionalCameraData>();
        uiCamData.renderType = CameraRenderType.Overlay;

        GameObject playerCamObj = new GameObject("PlayerCamera");
        playerCamObj.transform.SetParent(this.transform);
        var _playerCamera = playerCamObj.AddComponent<Camera>();
        _playerCamera.CopyFrom(_mainCamera);
        _playerCamera.clearFlags = CameraClearFlags.Nothing;

        int everythingMask = ~0;
        int layersToExclude = (1 << LayerMask.NameToLayer("Map")) | (1 << LayerMask.NameToLayer("IndicatorUI"));
        _playerCamera.cullingMask = everythingMask & ~layersToExclude;

        var playerCamData = _playerCamera.gameObject.GetOrAddComponent<UniversalAdditionalCameraData>();
        playerCamData.renderType = CameraRenderType.Overlay;

        mainCamData.cameraStack.Add(_uiCamera);     
        mainCamData.cameraStack.Add(_playerCamera);  
    }


    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll < 0f)
        {
            _currentStep = Mathf.Max(_currentStep - 1, 0);
            _targetZoom = _zoomSteps[_currentStep];
        }
        else if (scroll > 0f)
        {
            _currentStep = Mathf.Min(_currentStep + 1, _zoomSteps.Length - 1);
            _targetZoom = _zoomSteps[_currentStep];
        }

        if (_isLerpComplete)
        {
            _currentZoom = Mathf.MoveTowards(_currentZoom, _targetZoom, _zoomSpeed * Time.deltaTime);
        }
    }

    void LateUpdate()
    {
        if (_mode == Define.CameraMode.QuaterView)
        {
            if (_player == null || !_player.activeSelf) // IsValid() 대신 null 또는 activeSelf 체크
            {
                return;
            }

            Vector3 targetDelta = (_currentZoom <= _lastZoom) ? _nearDelta : _farDelta;
            _delta = Vector3.MoveTowards(_delta, targetDelta, _lerpSpeed * Time.deltaTime);

            if (Vector3.Distance(_delta, targetDelta) < 0.01f)
                _isLerpComplete = true;
            else
                _isLerpComplete = false;

            Vector3 zoomedOffset = _delta.normalized * _currentZoom;
            transform.position = _player.transform.position + zoomedOffset;
            transform.LookAt(_player.transform.position + Vector3.up);

            LateUpdateAction?.Invoke();
        }
    }

    public void SetQuaterView(Vector3 delta)
    {
        _mode = Define.CameraMode.QuaterView;
        _farDelta = delta;
    }
}