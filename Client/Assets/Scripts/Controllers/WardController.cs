using Google.Protobuf.Protocol;
using System.Collections;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static UnityEngine.GraphicsBuffer;

public class WardController : BaseController
{
    public int TeamIndex;

    private float elapsed = 0;
    public float _lifeTime = 15f; // �͵��� ����
    public float _destructionAnimationDuration = 3f; // �ı� �ִϸ��̼� ��� �ð�

    private FogOfWarVision _vision;

    private int _layer1Team;
    private int _layer2Team;


    [Header("UI Canvas ����")]
    public Canvas mainScreenCanvas; 
                                    

    Vector3 uiOffset = new Vector3(0, 1.3f, 0); 

    private GameObject _lifeBarInstance;
    private UI_WardLifeBar _lifeBarController;

    Canvas _canvas;
    CanvasGroup _cg = null;
    public bool IsVisible = true;
    public bool IsInBush = false;

    private void Awake()
    {
        if (mainScreenCanvas == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("MainCanvas");

            if(go != null)
            {
                mainScreenCanvas = go.GetComponent<Canvas>();
            }
        }

    }

    void Start()
    {
        Init();
        StartCoroutine(LifecycleRoutine());    
    }

    private RectTransform _rect;

    protected override void Init()
    {
        base.Init();

        // vision process
        _vision = gameObject.AddComponent<FogOfWarVision>();
        _vision.ViewDistance = 13f;

        string layer1Name = $"FogTeam1";
        string layer2Name = $"FogTeam2";
        _layer1Team = LayerMask.NameToLayer(layer1Name);
        _layer2Team = LayerMask.NameToLayer(layer2Name);

        if(TeamIndex == 1)
            gameObject.layer = _layer1Team;
        else
            gameObject.layer = _layer2Team;

        if (TeamIndex == Managers.Object.MyPlayer.ObjInfo.Player.Team)
        {
            Managers.Object.MyPlayer.View.WardIds.Add(Id);
            //Debug.Log("")
        }

        if (mainScreenCanvas != null /*&& wardLifeBarUIPrefab != null*/)
        {
            InitializeLifeBarUI();
        }

        _lifeBarController.SetMaxValue(_lifeTime);
        _rect = _lifeBarInstance.GetComponent<RectTransform>();

        IsHide = true;
        if (TeamIndex == Managers.Object.MyPlayer.ObjInfo.Player.Team || false == IsInBush)
            IsHide = false;

        //Debug.Log($"WardTeam: {TeamIndex}");
        //Debug.Log($"PlayerTeam: {Managers.Object.MyPlayer.ObjInfo.Player.Team}");
    }

    IEnumerator LifecycleRoutine()
    {
        while(true)
        {
            elapsed += Time.deltaTime;
            _lifeBarController.SetValue(_lifeTime - elapsed);
            if (elapsed > _lifeTime)
                break;

            yield return null;
        }

        if (_animator != null)
        {
            _vision.ViewDistance = 0.01f;
            _animator.SetTrigger("dead");
            if (_lifeBarInstance != null)
            {
                Destroy(_lifeBarInstance);
                _lifeBarInstance = null;
            }
            if (TeamIndex == Managers.Object.MyPlayer.ObjInfo.Player.Team)
            {
                Managers.Object.MyPlayer.View.WardIds.Remove(Id);
            }
            yield return new WaitForSeconds(_destructionAnimationDuration);
        }

        Managers.Object.Remove(Id);
    }

    private void InitializeLifeBarUI()
    {
        _lifeBarInstance = Managers.Resource.Instantiate("UI/SubItem/WardLifeBarCanvas", mainScreenCanvas.transform);

        if (_lifeBarInstance == null)
        {
            return;
        }

        _lifeBarController = _lifeBarInstance.GetComponent<UI_WardLifeBar>();
        if (_lifeBarController == null)
        {
            _lifeBarController = _lifeBarInstance.GetComponentInChildren<UI_WardLifeBar>();
        }

        if (_lifeBarController == null)
        {
            Destroy(_lifeBarInstance); 
            return;
        }
        _cg = _lifeBarInstance.GetComponentInChildren<CanvasGroup>();
        _canvas = _lifeBarInstance.GetComponent<Canvas>();

        // 와드UI 색상 지정
        if (TeamIndex == Managers.Object.MyPlayer.ObjInfo.Player.Team)
        {
            _lifeBarController.SetColor(new Color(0.2f, 0.5f, 1f, 1f));
        }
        else
        {
            _lifeBarController.SetColor(new Color(1f, 0f, 0f, 1f));
        }
    }

    void OnEnable()
    {
        Canvas.willRenderCanvases += UpdateWardPosition;
    }

    void OnDisable()
    {
        if(_rect != null)
            _rect.anchoredPosition = new Vector2(10000, 10000);
        Canvas.willRenderCanvases -= UpdateWardPosition;
    }

    private void UpdateWardPosition()
    {
        if (_rect == null)
            return;
        if (_lifeBarInstance == null || mainScreenCanvas == null || Camera.main == null)
        {
            _rect.anchoredPosition = new Vector2(10000, 10000);
            return;
        }

        Vector3 wardWorldPos = transform.position + uiOffset;
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(wardWorldPos);
        if (screenPoint.z <= 0)
        {
            _rect.anchoredPosition = new Vector2(10000, 10000);
            return;
        }

        Vector2 screenDirection = Vector2.up;

        float screenOffset = 35f;
        Vector2 screenPos = new Vector2(screenPoint.x, screenPoint.y) + (screenDirection * screenOffset);

        screenPos = new Vector2(
            Mathf.Round(screenPos.x * 2f) / 2f,
            Mathf.Round(screenPos.y * 2f) / 2f
        );

        if (IsOnScreen(screenPos))
        {
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mainScreenCanvas.transform as RectTransform,
                screenPos,
                null,
                out localPos
            );

            _rect.anchoredPosition = localPos;
        }
        else
        {
            _rect.anchoredPosition = new Vector2(10000, 10000);
        }
    }

    private bool IsOnScreen(Vector3 screenPoint)
    {
        // 1. 카메라 뒤에 있음
        if (screenPoint.z < 0f) return false;

        // 2. 화면 경계 밖에 있음 (약간의 여유 공간 추가)
        float margin = 100f; // 픽셀 단위 여유공간
        if (screenPoint.x < -margin || screenPoint.x > Screen.width + margin ||
            screenPoint.y < -margin || screenPoint.y > Screen.height + margin)
        {
            return false;
        }

        return true;
    }
    
    public void SetWardLifeBarActive(bool isActive)
    {
        if(null == _lifeBarInstance) return;
        _lifeBarInstance.SetActive(isActive);
    }

    public void SetVisible(bool isVisible)
    {
        if (TeamIndex == Managers.Object.MyPlayer.ObjInfo.Player.Team)
            isVisible = true;

        if (_canvas == null)
            return;
        
        _canvas.enabled = isVisible;
        foreach (Renderer r in gameObject.GetComponentsInChildren<Renderer>())
        {
            if (r == null) continue;
            r.enabled = isVisible;
        }
        if (_lifeBarInstance == null || _cg == null)
            return;
        if (_cg != null)
            _cg.alpha = isVisible ? 1f : 0f; 
    }

    public void LateUpdate()
    {
        if(IsInBush)
            SetVisible(IsVisible);
    }
}
