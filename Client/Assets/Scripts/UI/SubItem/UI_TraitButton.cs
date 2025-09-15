using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_TraitButton : UI_Base
{
    enum Images { Image }
    enum GameObjects { Text }

    private Material _material;

    private static UI_TraitButton _currentSelected;

    public Action<bool> OnSelected = null;

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(GameObjects));

        BindEvent(gameObject, OnClickedEvent, Define.UIEvent.Click);

        _material = GetImage((int)Images.Image).material;

        ApplyBlendValue(1f);
        SetTextActivate(false);
    }

    private void Awake()
    {
        Init();
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    void SetTextActivate(bool activate)
    {
        GetObject((int)GameObjects.Text).SetActive(activate);
    }

    // 이 오브젝트를 선택/해제하는 함수 
    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            // 이전에 선택된 객체가 있다면, 그 객체를 흑백으로 돌립니다.
            if (_currentSelected != null && _currentSelected != this)
            {
                _currentSelected.SetSelected(false); // 재귀 호출 방지를 위해 false
            }
            // 컬러
            ApplyBlendValue(0f);
            SetTextActivate(true);
            OnSelected?.Invoke(true);
            _currentSelected = this; 
        }
        else
        {
            // 선택 해제 시 흑백
            ApplyBlendValue(1f);
            SetTextActivate(false);
            OnSelected?.Invoke(false);
            if (_currentSelected == this)
            {
                _currentSelected = null; 
            }
        }
    }

    private void ApplyBlendValue(float blendValue)
    {
        if (_material == null)
            return;

        _material.SetFloat("_IsNonColor", blendValue);
    }

    private void OnClickedEvent(PointerEventData data)
    {
        SetSelected(true);
    }
}
