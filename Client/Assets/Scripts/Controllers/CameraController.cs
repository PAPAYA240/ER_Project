using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.GraphicsBuffer;

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

    public void SetPlayer(GameObject player) { _player = player; }

    void Start()
    {
        PhysicsRaycaster raycaster = GetComponent<PhysicsRaycaster>();

        if (raycaster == null)
            gameObject.AddComponent<PhysicsRaycaster>();

        Camera.main.cullingMask |= (1 << LayerMask.NameToLayer("FX"));

        _currentZoom = _zoomSteps[_currentStep];
        _targetZoom = _currentZoom;
        _lastZoom = _zoomSteps[_zoomSteps.Length - 1];
        _zoomSpeed = 6f;
        _lerpSpeed = 15f;
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
            if (_player.IsValid() == false)
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
        }
    }

    public void SetQuaterView(Vector3 delta)
    {
        _mode = Define.CameraMode.QuaterView;
        _farDelta = delta;
    }
}

