using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Scene : UI_Base
{
    const int _originHeight = 617;
    const int _originWidth = 1232;
    protected CanvasScaler _scaler;

    public override void Init()
	{
		Managers.UI.SetCanvas(gameObject, false);
        _scaler = gameObject.GetComponent<CanvasScaler>();
    }

    virtual protected void UpdateScale()
    {
        if (null != _scaler)
        {
            float widthRatio = (float)Screen.width / _originWidth;
            float heightRatio = (float)Screen.height / _originHeight;
            _scaler.scaleFactor = Mathf.Min(widthRatio, heightRatio);
        }
    }
}
