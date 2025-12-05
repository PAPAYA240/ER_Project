using System;
using System.Collections;
using UnityEngine;

public class BeaconController : BaseController
{
    [Header("Beacon Settings")]
    [SerializeField] private Renderer gaugeRenderer;

    private float captureSpeed = 0.203f;

    private Material gaugeMaterial;
    private int currentCapturingTeam = 0;
    private int currentOwningTeam = 0;
    private float currentCaptureAmount = 0f;
    private bool isCapturing = false;
    private Coroutine captureCoroutine;

    private static readonly int ShaderProgress = Shader.PropertyToID("_CaptureProgress");
    private static readonly int ShaderTeam = Shader.PropertyToID("_CaptureTeam");
    private static readonly int ShaderOwningTeam = Shader.PropertyToID("_OwningTeam");

    public System.Action<int, float> OnCaptureProgressChanged;
    public System.Action<int> OnCaptureCompleted;
    public System.Action OnCaptureFailed;

    private const string _effectPath = "effects/prefab/Common/Beacon_Complete";
    private bool _idleSound = false;

    [Header("UI")]
    [SerializeField] private float interactDuration = 5f;
    [SerializeField] private string interactDescribe = "증폭 장치를 점령 중입니다.";

    public void Begin() => Managers.Object.MyPlayer.UI.InteractionCharge.Begin(interactDuration, interactDescribe);
    public void Complete() => Managers.Object.MyPlayer.UI.InteractionCharge.Complete();
    public void Cancel() => Managers.Object.MyPlayer.UI.InteractionCharge.Cancel();

    void Start()
    {
        if (gaugeRenderer != null)
        {
            gaugeMaterial = gaugeRenderer.material; // .material 사용하면 인스턴스 생성됨
            ResetCaptureState();
        }
        else
        {
            Debug.LogError("GaugeRenderer is not assigned!", this);
        }
    }

    protected void Update()
    {
        if(!_idleSound)
            _idleSound = Managers.Object.MyPlayer?.Sound.PlayLoopSound("Beacon_Idle", true, transform.position);
    }
    public void StartCapture(int team)
    {
        if (team != 1 && team != 2) return;

        if (isCapturing && currentCapturingTeam == team) return;

        if (captureCoroutine != null)
        {
            StopCoroutine(captureCoroutine);
        }

        currentCaptureAmount = 0f;
        currentCapturingTeam = team;
        isCapturing = true;

        if (gaugeMaterial != null)
        {
            int shaderTeamValue = GetShaderTeamValue(team);
            gaugeMaterial.SetInt(ShaderTeam, shaderTeamValue);
        }

        captureCoroutine = StartCoroutine(CaptureRoutine());
    }

    public void CompleteCapture(int Team)
    {
        Managers.FX.Effect.PlayEffect(_effectPath, transform, 3.0f, new Vector3(0, 1.5f, 0));

        currentCaptureAmount = 1f;
        currentOwningTeam = Team;
        isCapturing = false;

        UpdateShaderProperties();
        OnCaptureCompleted?.Invoke(Team);
    }

    public void FailCapture()
    {
        if (!isCapturing) return;

        if (captureCoroutine != null)
        {
            StopCoroutine(captureCoroutine);
            captureCoroutine = null;
        }

        currentCaptureAmount = currentOwningTeam > 0 ? 1f : 0f;
        isCapturing = false;
        UpdateShaderProperties();

        OnCaptureFailed?.Invoke();
    }

    public void ResetBeacon()
    {
        if (captureCoroutine != null)
        {
            StopCoroutine(captureCoroutine);
            captureCoroutine = null;
        }

        ResetCaptureState();
    }

    private IEnumerator CaptureRoutine()
    {
        Managers.Object.MyPlayer.Sound.GetEffect3D("Beacon_Active", transform.position);
        Managers.Sound.StopLoopSound("Beacon_Idle");

        while (isCapturing && currentCaptureAmount < 1f)
        {
            currentCaptureAmount += captureSpeed * Time.deltaTime;
            currentCaptureAmount = Mathf.Clamp01(currentCaptureAmount);

            UpdateShaderProperties();
            OnCaptureProgressChanged?.Invoke(currentCapturingTeam, currentCaptureAmount);

            yield return null;
        }
    }

    private void UpdateShaderProperties()
    {
        if (gaugeMaterial != null)
        {
            gaugeMaterial.SetFloat(ShaderProgress, currentCaptureAmount);
            int shaderOwningTeamValue = currentOwningTeam > 0 ? GetShaderTeamValue(currentOwningTeam) : 0;
            gaugeMaterial.SetInt(ShaderOwningTeam, shaderOwningTeamValue);

            if (isCapturing)
            {
                // 점령 중일 때는 점령 시도하는 팀 색상
                int shaderTeamValue = GetShaderTeamValue(currentCapturingTeam);
                gaugeMaterial.SetInt(ShaderTeam, shaderTeamValue);
            }
            else
            {
                // 점령 중이 아닐 때는 소유 팀 색상 (소유 팀이 없으면 0)
                int shaderTeamValue = currentOwningTeam > 0 ? GetShaderTeamValue(currentOwningTeam) : 0;
                gaugeMaterial.SetInt(ShaderTeam, shaderTeamValue);
            }
        }
    }

    private int GetShaderTeamValue(int capturingTeam)
    {
        int myTeam = Managers.Info.Team;
        int result = capturingTeam == myTeam ? 1 : 2;
        return result;
    }

    private void ResetCaptureState()
    {
        currentCaptureAmount = 0f;
        currentCapturingTeam = 0;
        currentOwningTeam = 0;
        isCapturing = false;

        UpdateShaderProperties();
    }

    void OnDestroy()
    {
        if (captureCoroutine != null)
        {
            StopCoroutine(captureCoroutine);
        }

        if (gaugeMaterial != null)
        {
            Destroy(gaugeMaterial);
        }
    }
}