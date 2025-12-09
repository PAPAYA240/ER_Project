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

    private Renderer _renderer;
    private Material _outlineMaterialInstance;
    private Material[] _originalMaterials;

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
        Renderer[] allChildRenderers = GetComponentsInChildren<Renderer>(true);
        Renderer[] targetRenderers = allChildRenderers
            .Where(r => r.gameObject.name == "Cobalt_OBJ_Turbine_01_Base")
            .ToArray();

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            //Debug.LogWarning($"{gameObject.name}에 렌더러가 없습니다. OnBeacon을 적용할 수 없습니다.");
            enabled = false;
            return;
        }

        _renderer = targetRenderers[0];

        if (targetRenderers.Length > 1)
        {
            //Debug.LogWarning($"'{gameObject.name}'에 'Cobalt_OBJ_Turbine_01_Base' 이름의 렌더러가 {targetRenderers.Length}개 있습니다. 첫 번째만 사용합니다.");
        }

        Shader outlineShader = Shader.Find("Custom/BeaconOutline");
        if (outlineShader == null)
        {
            //Debug.LogError("Custom/BeaconOutline 셰이더를 찾을 수 없습니다!");
            enabled = false;
            return;
        }
        _outlineMaterialInstance = new Material(outlineShader);

        _outlineMaterialInstance.SetFloat("_Width", _beaconOutlineWidth);
        _outlineMaterialInstance.SetColor("_OutlineColor", _beaconOutlineColor);

        // 원본 머티리얼 저장 (단일 배열)
        if (_renderer != null)
        {
            _originalMaterials = _renderer.sharedMaterials;
        }
        else
        {
            _originalMaterials = new Material[0];
        }
    }

    private void InitializeCursorTextures()
    {
        _cursorDefault = Managers.Resource?.Load<Texture2D>("Cursor/Cursor_01") ?? Resources.Load<Texture2D>("Cursor/Cursor_01");
        _cursorEnemy = Managers.Resource?.Load<Texture2D>("Cursor/Cursor_12") ?? Resources.Load<Texture2D>("Cursor/Cursor_12");
    }


    private void ApplyOutlineEffect()
    {
        if (_outlineMaterialInstance == null || _renderer == null) return;

        // 현재 머티리얼에 아웃라인 머티리얼이 없는 경우 추가
        if (!_renderer.materials.Contains(_outlineMaterialInstance))
        {
            _renderer.materials = _renderer.materials.Append(_outlineMaterialInstance).ToArray();
        }
    }

    private void RemoveOutlineEffect(bool isDestroying = false)
    {
        if (_renderer == null || _originalMaterials == null) return;

        // 원본 머티리얼로 복원
        _renderer.materials = _originalMaterials;
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
        if (_renderer == null || _isDeactivated)
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