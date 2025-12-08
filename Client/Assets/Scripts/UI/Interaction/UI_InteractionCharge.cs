using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UI_Minimap;
using static UI_Stat;

public class UI_InteractionCharge : UI_Base
{
    [Header("UI References")]
    [SerializeField] private Image chargingBarImage;            // ChargingBarImage
    [SerializeField] private TextMeshProUGUI describeText;      // DescribeText
    [SerializeField] private TextMeshProUGUI remainTimeText;    // RemainTime

    [Header("Default Settings")]
    [SerializeField] private string defaultDescribe = "디플로잉 루프 가동 중";

    private float _totalDuration = 3f;
    private float _elapsed;
    private bool _isPlaying;

    private void Awake()
    {
        gameObject.SetActive(false);

        if (chargingBarImage != null)
            chargingBarImage.fillAmount = 0f;

        if (describeText != null)
            describeText.text = defaultDescribe;

        if (remainTimeText != null)
            remainTimeText.text = "";
    }

    private void Update()
    {
        if (!_isPlaying)
            return;

        _elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(_elapsed / _totalDuration);

        // progress bar
        if (chargingBarImage != null)
            chargingBarImage.fillAmount = t;

        // remain time
        float remain = Mathf.Max(0f, _totalDuration - _elapsed);
        if (remainTimeText != null)
            remainTimeText.text = remain.ToString("0.0");

        if(remain <= 0)
            Complete();
    }

    /// <summary>
    /// 외부에서 상호작용 시작할 때 호출.
    /// </summary>
    public void Begin(float duration, string describe = null)
    {
        _totalDuration = Mathf.Max(0.01f, duration);
        _elapsed = 0f;
        _isPlaying = true;

        if (chargingBarImage != null)
            chargingBarImage.fillAmount = 0f;

        if (describeText != null)
            describeText.text = string.IsNullOrEmpty(describe) ? defaultDescribe : describe;

        if (remainTimeText != null)
            remainTimeText.text = duration.ToString("0.0");

        gameObject.SetActive(true);
    }

    /// <summary>
    /// 외부에서 상호작용이 완료되었다고 알려줄 때.
    /// </summary>
    public void Complete()
    {
        _isPlaying = false;

        // 필요하면 여기서 100%로 만들기
        if (chargingBarImage != null)
            chargingBarImage.fillAmount = 1f;

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 외부에서 상호작용이 취소되었다고 알려줄 때.
    /// </summary>
    public void Cancel()
    {
        _isPlaying = false;

        // UI 숨기기
        gameObject.SetActive(false);
    }

    public override void Init()
    {
    }
}

