using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_TraitButton : Monobehaviour
{
    enum Images { Image }
    enum GameObjects { Text }

    private Material _material;

    [SerializeField]
    TraitType _traitType;

    public static UI_TraitButton CurrentSelected { get; private set; }

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

    // �� ������Ʈ�� ����/�����ϴ� �Լ� 
    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            // ������ ���õ� ��ü�� �ִٸ�, �� ��ü�� ��鵹��
            if (CurrentSelected != null && CurrentSelected != this)
            {
                CurrentSelected.SetSelected(false); // ��� ȣ�� ������ ���� false
            }
            // �÷�
            ApplyBlendValue(0f);
            SetTextActivate(true);
            OnSelected?.Invoke(true);
            CurrentSelected = this; 
        }
        else
        {
            // ���� ���� �� ���
            ApplyBlendValue(1f);
            SetTextActivate(false);
            OnSelected?.Invoke(false);
            if (CurrentSelected == this)
            {
                CurrentSelected = null; 
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
        if (Managers.Info.IsReady)
            return;

        SetSelected(true);

        PickScene ps =  Managers.Scene.CurrentScene as PickScene;
        if (ps == null)
            return;
        
        C_Trait traitPacket = new C_Trait();
        traitPacket.TraitType = _traitType;
        traitPacket.PickIdx = ps.PickIdx;
        Managers.Network.Send(traitPacket);

        ps.ChangeTraitImage(_traitType, ps.PickIdx);
    }
}
