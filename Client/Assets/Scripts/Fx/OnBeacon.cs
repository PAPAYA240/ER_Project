using Google.Protobuf.Protocol;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OnBeacon : MonoBehaviour
{
    private Color _beaconOutlineColor = new Color(0.5f, 0.8f, 1.0f, 1.0f);
    private float _beaconOutlineWidth = 0.005f;

    private Texture2D _cursorDefault;
    private Texture2D _cursorEnemy;

    private Renderer[] _allRenderers;
    private Material _outlineMaterialInstance;
    private Material[][] _originalMaterials;

    private bool _isHighlighted = false;
    private bool _isDeactivated = false;

    void Awake()
    {
        InitializeRenderersAndMaterials();
        InitializeCursorTextures();
    }

    private void OnMouseEnter()
    {
        if (_isDeactivated) return;
        if (!CheckHighlightCondition())
        {
            SetCursor(false);
            return;
        }

        if (_isHighlighted) return;

        ApplyOutlineEffect();
        SetCursor(true);
        _isHighlighted = true;
    }

    private void OnMouseExit()
    {
        if (_isDeactivated) return;
        if (!_isHighlighted) return;

        RemoveOutlineEffect();
        SetCursor(false);
        _isHighlighted = false;
    }

    void OnDestroy()
    {
        if (_outlineMaterialInstance != null)
        {
            Destroy(_outlineMaterialInstance);
        }
        RemoveOutlineEffect(true);
    }

    private void InitializeRenderersAndMaterials()
    {
        _allRenderers = GetComponentsInChildren<Renderer>();

        if (_allRenderers == null || _allRenderers.Length == 0)
        {
            Debug.LogWarning($"{gameObject.name}에 렌더러가 없습니다. OnBeacon을 적용할 수 없습니다.");
            enabled = false;
            return;
        }

        Shader outlineShader = Shader.Find("Custom/BeaconOutline");
        if (outlineShader == null)
        {
            Debug.LogError("Custom/BeaconOutline 셰이더를 찾을 수 없습니다. 프로젝트에 이 셰이더 파일이 있고 이름이 'Custom/BeaconOutline'인지 확인해주세요!");
            enabled = false;
            return;
        }
        _outlineMaterialInstance = new Material(outlineShader);

        _outlineMaterialInstance.SetFloat("_Width", _beaconOutlineWidth);
        _outlineMaterialInstance.SetColor("_OutlineColor", _beaconOutlineColor);

        _originalMaterials = new Material[_allRenderers.Length][];
        for (int i = 0; i < _allRenderers.Length; i++)
        {
            if (_allRenderers[i] != null)
            {
                _originalMaterials[i] = _allRenderers[i].sharedMaterials;
            }
            else
            {
                _originalMaterials[i] = new Material[0];
            }
        }
    }

    private void InitializeCursorTextures()
    {
        _cursorDefault = Managers.Resource?.Load<Texture2D>("Cursor/Cursor_01") ?? Resources.Load<Texture2D>("Cursor/Cursor_01");
        _cursorEnemy = Managers.Resource?.Load<Texture2D>("Cursor/Cursor_05") ?? Resources.Load<Texture2D>("Cursor/Cursor_05");

        if (_cursorDefault == null) Debug.LogWarning("기본 커서 텍스처를 찾을 수 없습니다. Resources/Cursor/Cursor_01 경로 확인.");
        if (_cursorEnemy == null) Debug.LogWarning("적 커서 텍스처를 찾을 수 없습니다. Resources/Cursor/Cursor_05 경로 확인.");
    }


    private void ApplyOutlineEffect()
    {
        if (_outlineMaterialInstance == null) return;

        foreach (var renderer in _allRenderers)
        {
            if (renderer == null) continue;

            if (!renderer.materials.Contains(_outlineMaterialInstance))
            {
                renderer.materials = renderer.materials.Append(_outlineMaterialInstance).ToArray();
            }
        }
    }

    private void RemoveOutlineEffect(bool isDestroying = false)
    {
        if (_allRenderers == null || _originalMaterials == null) return;

        for (int i = 0; i < _allRenderers.Length; i++)
        {
            var renderer = _allRenderers[i];
            if (renderer == null || i >= _originalMaterials.Length || _originalMaterials[i] == null) continue;

            renderer.materials = _originalMaterials[i];
        }
    }

    private void SetCursor(bool isEnemy)
    {
        if (isEnemy && _cursorEnemy != null)
        {
            Cursor.SetCursor(_cursorEnemy, Vector2.zero, CursorMode.Auto);
        }
        else if (_cursorDefault != null)
        {
            Cursor.SetCursor(_cursorDefault, Vector2.zero, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    private bool CheckHighlightCondition()
    {
        if (_allRenderers == null || _allRenderers.Length == 0 || _isDeactivated)
            return false;

        return true;
    }

    public void Deactivate()
    {
        _isDeactivated = true;

        if (_isHighlighted)
        {
            RemoveOutlineEffect();
            SetCursor(false);
            _isHighlighted = false;
        }
    }
}