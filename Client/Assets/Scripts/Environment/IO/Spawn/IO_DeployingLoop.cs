using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class IO_DeployingLoop : MonoBehaviour
{
    [Header("DeployingLoop Settings")]
    public int id;
    public bool side;

    [SerializeField] private Transform _lookTarget;
    public Vector3 GetLookTargetPosition() => _lookTarget != null ? _lookTarget.position : transform.position;

    [Header("State Flags")]
    private readonly int ActivationPhase = 2;

    private bool _isUsable = false;
    public bool IsUsable => _isUsable;

    private bool _isPlayerInside = false;
    public bool IsPlayerInside => _isPlayerInside;

    private bool _onHover = false;

    [Header("Cursor")]
    private Texture2D _cursorDefault;
    private Texture2D _cursorOperate;
    private LayerMask _deployingLoopMask;

    [Header("Outline")]
    [SerializeField] private Renderer targetRenderer;   
    [SerializeField] private Material outlineMaterial;

    private Material[] _originalMats;
    private Material[] _withOutlineMats;

    [Header("Effect Color")]
    [SerializeField] private ParticleSystem effectParticle;
    [SerializeField] private Color usableColor = new Vector4(0, 0.20f, 0.26f, 0.80f);
    [SerializeField] private Color unusableColor = new Vector4(0.67f, 0f, 0f, 0.80f);

    [Header("UI")]
    [SerializeField] private float interactDuration = 3f;
    [SerializeField] private string interactDescribe = "디플로잉 루프 가동 중";

    public void Begin() => Managers.Object.MyPlayer.UI.InteractionCharge.Begin(interactDuration, interactDescribe);
    public void Complete() => Managers.Object.MyPlayer.UI.InteractionCharge.Complete();
    public void Cancel() => Managers.Object.MyPlayer.UI.InteractionCharge.Cancel();

    void Awake()
    {
        InitializeRenderersAndMaterials();
        InitializeCursorTextures();
        SetEffectColor(_isUsable);

        _deployingLoopMask = LayerMask.GetMask("DeployingLoop");
    }

    private void InitializeRenderersAndMaterials()
    {
        if (targetRenderer != null && outlineMaterial != null)
        {
            _originalMats = targetRenderer.sharedMaterials;

            _withOutlineMats = new Material[_originalMats.Length + 1];
            for (int i = 0; i < _originalMats.Length; ++i)
                _withOutlineMats[i] = _originalMats[i];

            _withOutlineMats[_withOutlineMats.Length - 1] = outlineMaterial;
        }
    }

    private void InitializeCursorTextures()
    {
        _cursorDefault = Managers.Resource?.Load<Texture2D>("Cursor/Cursor_01") ?? Resources.Load<Texture2D>("Cursor/Cursor_01");
        _cursorOperate = Managers.Resource?.Load<Texture2D>("Cursor/Cursor_12") ?? Resources.Load<Texture2D>("Cursor/Cursor_12");

        if (_cursorDefault == null)
            Debug.LogWarning("_cursorDefault 텍스처를 찾을 수 없습니다. Resources/Cursor/Cursor_01 경로 확인.");
        if (_cursorOperate == null)
            Debug.LogWarning("_cursorOperate 텍스처를 찾을 수 없습니다. Resources/Cursor/Cursor_12 경로 확인.");
    }

    void Update()
    {
        if(!_isUsable && CheckUsable())
        {
            _isUsable = true;
            SetEffectColor(_isUsable);
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _deployingLoopMask))
        {
            GameObject go = hit.collider.gameObject;

            if (this.gameObject == go)
            {
                if(!_onHover)
                {
                    _onHover = true;
                    OnHover();
                }
            }
        }
        else
        {
            if(_onHover)
            {
                _onHover = false;
                OnUnhover();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInChildren<MyPlayerController>() != null)
        {
            _isPlayerInside = true;
            // Hint UI 켜기, 상호작용 키 안내 등
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInChildren<MyPlayerController>() != null)
        {
            _isPlayerInside = false;
            // Hint UI 끄기
        }
    }

    public void OnHover()
    {
        // 커서 변경
        if (_cursorOperate != null)
            Cursor.SetCursor(_cursorOperate, Vector2.zero, CursorMode.Auto);

        // 아웃라인 켜기 
        if (targetRenderer != null && _withOutlineMats != null)
            targetRenderer.materials = _withOutlineMats;
    }

    public void OnUnhover()
    {
        // 기본 커서 복구
        if (_cursorDefault != null)
            Cursor.SetCursor(_cursorDefault, Vector2.zero, CursorMode.Auto);

        // 아웃라인 끄기
        if (targetRenderer != null && _originalMats != null)
            targetRenderer.materials = _originalMats;
    }

    private bool CheckUsable()
    {
        if(Managers.Object.MyPlayer == null)
            return false;

        //if(Managers.Object.MyPlayer.CurPhase < ActivationPhase)
        //    return false;

        return true;
    }

    private void SetEffectColor(bool isUsable)
    {
        if (effectParticle == null)
            return;

        var main = effectParticle.main;

        if(isUsable)
            main.startColor = usableColor;
        else
            main.startColor = unusableColor;
    }
}

