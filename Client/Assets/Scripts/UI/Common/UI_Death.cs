using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Death : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] Image tensImage;          // 십의 자리
    [SerializeField] Image onesImage;          // 일의 자리
    [SerializeField] Sprite[] numberSprites;   // 0~9 순서대로

    public void Bind(UI_PlayerInterface respawn)
    {
        // 구독
        respawn.OnSecondsChanged -= UpdateNumberUI;
        respawn.OnSecondsChanged += UpdateNumberUI;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void UpdateNumberUI(int seconds)
    {
        seconds = Mathf.Clamp(seconds, 0, 99);

        if (seconds < 10)
        {
            tensImage.gameObject.SetActive(false);
            onesImage.sprite = numberSprites[seconds];
        }
        else
        {
            tensImage.gameObject.SetActive(true);
            tensImage.sprite = numberSprites[seconds / 10];
            onesImage.sprite = numberSprites[seconds % 10];
        }
    }
}
