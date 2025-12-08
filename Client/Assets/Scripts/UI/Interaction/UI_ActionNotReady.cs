using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UI_Minimap;
using static UI_Stat;

public class UI_ActionNotReady : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject root;               // 켜고 끌 패널
    [SerializeField] private TextMeshProUGUI describeText;   

    [Header("Default Settings")]
    [SerializeField] private float visibleTime = 1.5f;   
    [SerializeField] private string defaultDescribe = "전투 중에는 휴식을 할 수 없습니다.";

    private Coroutine _hideCo;

    /// <summary>
    /// 행동 불가일 때 호출
    /// </summary>
    public void Show(string describe)
    {
        if (describeText != null)
            describeText.text = string.IsNullOrEmpty(describe) ? defaultDescribe : describe;

        // 이미 떠 있는 상태면 깜빡이게
        if (gameObject.activeSelf)
            StartCoroutine(BlinkOnce());
        else
        {
            root.SetActive(true);
            StartHideCoroutine();
        }
    }

    private void StartHideCoroutine()
    {
        if (_hideCo != null)
            StopCoroutine(_hideCo);
        _hideCo = StartCoroutine(AutoHide());
    }

    private IEnumerator BlinkOnce()
    {
        root.SetActive(false);
        yield return new WaitForSeconds(0.05f);

        root.SetActive(true);
        StartHideCoroutine();
    }

    private IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(visibleTime);
        root.SetActive(false);
        _hideCo = null;
    }
}

