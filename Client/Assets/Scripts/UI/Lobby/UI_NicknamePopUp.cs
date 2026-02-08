using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class UI_NicknamePopUp : Monobehaviour
{
    [SerializeField] InputField _nicknameInput;
    [SerializeField] Button _skipButton;
    [SerializeField] Button _confirmButton;

    public event Action<string> OnConfirm;
    public event Action OnSkip;

    bool _focusOff = false;

    public override void Init()
    {
        _skipButton.onClick.AddListener(() => OnSkip?.Invoke());
        _confirmButton.onClick.AddListener(() => OnConfirm?.Invoke(_nicknameInput.text));
    }

    private void Awake()
    {
        Init();
    }

    private void Start()
    {
        _nicknameInput.Select();
        _nicknameInput.ActivateInputField();
    }

    private void Update()
    {
        if (_focusOff)
        {
            _nicknameInput.Select();
            _nicknameInput.ActivateInputField();
        }
    }

    public void OnBlurClick(BaseEventData data)
    {
        _focusOff = true; // Update에서 포커스 처리
    }
}
