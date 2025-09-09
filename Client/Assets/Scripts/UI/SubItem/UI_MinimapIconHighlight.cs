using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_MinimapIconHighlight : UI_Base
{
    [SerializeField]
    Vector2 _minSize;
    [SerializeField]
    Vector2 _maxSize;

    [SerializeField]
    float _hightlightSpeed;

    float _ratio = 0;

    RectTransform _rectTransform;
    Image _image;


    public override void Init()
    {
        _rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
        _rectTransform.sizeDelta = _minSize;
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
        _ratio = Mathf.Min(1, _ratio + Time.deltaTime * _hightlightSpeed);

        _rectTransform.sizeDelta = Vector2.Lerp(_minSize, _maxSize, _ratio);

        Color curColor = _image.color;
        curColor.a = 1f - _ratio;
        _image.color = curColor;

        if (_ratio == 1)
            _ratio = 0;

    }
}
