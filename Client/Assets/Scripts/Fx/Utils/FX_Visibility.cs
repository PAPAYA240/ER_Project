using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class FX_Visibility : MonoBehaviour
{
    private Renderer[] _renderers;
    private bool _visible = true;
    private bool _initialized = false;

    private void OnEnable()
    {
        RefreshRenderers();
    }


    public void RefreshRenderers()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _initialized = true;
    }

    public void SetVisible(bool visible)
    {
        if (!_initialized)
            RefreshRenderers();

        if (_renderers != null)
        {
            foreach (var r in _renderers)
            {
                if (r != null)
                    r.enabled = visible;
            }
        }

        Debug.Log($"@ FX_Visibility : Owner - {gameObject.name}, Visible - {visible} ");
    }

    public bool IsVisible => _visible;
}