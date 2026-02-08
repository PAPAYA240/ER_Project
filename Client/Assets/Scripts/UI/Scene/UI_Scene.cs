using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Scene : Monobehaviour
{
    public override void Init()
	{
		Managers.UI.SetCanvas(gameObject, false);
        _scaler = gameObject.GetComponent<CanvasScaler>();
    }
}
