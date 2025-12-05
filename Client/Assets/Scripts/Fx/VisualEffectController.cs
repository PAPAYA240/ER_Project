using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VisualEffectController : MonoBehaviour
{
    enum HighlightMode
    {
        None,
        Outline,
        Bush,
        Bush_Invisible,
    }

    private Renderer[] myRenderers;
    private Material outlineMaterial;
    private Material[][] originalMaterials;

    private Material _bushMaterial;
    private Transform _lodTransform;
    private Dictionary<Renderer, Material[]> _originalMaterialsDict = new Dictionary<Renderer, Material[]>();

    // 커서
    private Texture2D _cursorDefault;
    private Texture2D _cursorEnemy;
    private HighlightMode _mode  = HighlightMode.None;

    public CreatureController Owner { get; set; }
    void Start()
    {
        InitCursor();
        InitOutline();
        InitBushRenderSetting();
    }
 
    void Update()
    {
        bool hitThisObject = false;
        MyPlayerController mpc = Managers.Object.MyPlayer;
        if (mpc == null)
            return;

        if(mpc.GetAttackableUnderCursor() == Owner.gameObject)
            hitThisObject = true;

        if (Owner?.State == CreatureState.Dead)
        {
            OnMouseExit();
            return;
        }

        if (hitThisObject && _mode == HighlightMode.None)
            OnMouseEnter();
        else if (!hitThisObject && _mode == HighlightMode.Outline)
            OnMouseExit();
    }

    #region Bush
    // 렌더러 비활성화
    public void MakeInvisible()
    {
        if (_mode == HighlightMode.Bush_Invisible)
            return;

        _mode = HighlightMode.Bush_Invisible;
        Renderer[] r = _lodTransform.GetComponentsInChildren<Renderer>();
        foreach (Renderer rr in r)
        {
            rr.enabled = false;
        }
    }

    // 렌더러 활성화
    public IEnumerator MakeVisible(float duration = 0f)
    {
        if (_mode == HighlightMode.None)
            yield break;

        yield return new WaitForSeconds(duration);

        _mode = HighlightMode.None;
        Renderer[] renderers = _lodTransform.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;

            // 각 Renderer의 원본 Material 복원
            if (_originalMaterialsDict.TryGetValue(renderer, out Material[] originalMaterials))
            {
                renderer.materials = originalMaterials;
            }
        }
    }

    public void ChangeBushRenderer()
    {
        if(_mode == HighlightMode.Bush)
            return;

        _mode = HighlightMode.Bush;
        Renderer[] renderers = _lodTransform.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;

            int materialCount = renderer.sharedMaterials.Length;
            Material[] ghostMaterials = new Material[materialCount];
            for (int i = 0; i < materialCount; i++)
            {
                ghostMaterials[i] = _bushMaterial;
            }
            renderer.sharedMaterials = ghostMaterials;
        }
    }
    #endregion

    private bool CheckCondition()
    {
        if (myRenderers == null)
            return false;

        if (Owner is MonsterController monster)
        {
            if (Managers.Object.MyPlayer.ObjInfo.Player.Team == monster.MonsterTeam)
                return false;
        }
        return true;
        
    }
    public void OnMouseEnter()
    {
        if (!CheckCondition())
            return;

        if (_mode != HighlightMode.None)
            return;

        _mode = HighlightMode.Outline;
        foreach (var renderer in myRenderers)
        {
            if (renderer == null) 
                continue;
            Material[] newMaterials = renderer.materials;
            renderer.materials = newMaterials.Append(outlineMaterial).ToArray();
        }
        Cursor.SetCursor(_cursorEnemy, Vector2.zero, CursorMode.Auto);
    }

    public void OnMouseExit()
    {
        if (myRenderers == null) 
            return;
        if (_mode != HighlightMode.Outline)
            return;

        _mode = HighlightMode.None;
        foreach (var renderer in myRenderers)
        {
            if (renderer == null) continue;
            Material[] materialsToKeep = renderer.materials.Where(m => m != outlineMaterial).ToArray();
            for (int i = 0; i < myRenderers.Length; i++)
            {
                if (myRenderers[i] != null)
                    myRenderers[i].materials = originalMaterials[i];
            }
        }
        Cursor.SetCursor(_cursorDefault, Vector2.zero, CursorMode.Auto);
    }

    #region init
    private void InitOutline()
    {
        myRenderers = GetComponentsInChildren<Renderer>();
        outlineMaterial = Resources.Load<Material>("materials/Outline/Outline");
        outlineMaterial = new Material(Shader.Find("Custom/Outline_Shader"));
        if (outlineMaterial == null || myRenderers == null || myRenderers.Length == 0)
            return;

        originalMaterials = new Material[myRenderers.Length][];
        for (int i = 0; i < myRenderers.Length; i++)
            originalMaterials[i] = myRenderers[i].sharedMaterials;

    }
    private void InitCursor()
    {
        _cursorDefault = Managers.Resource.Load<Texture2D>("Cursor/Cursor_01");
        _cursorEnemy = Managers.Resource.Load<Texture2D>("Cursor/Cursor_05");
    }
    private void InitBushRenderSetting()
    {
        _bushMaterial = Resources.Load<Material>("Material/ghostMaterial");

        foreach (Transform child in Owner.transform)
        {
            if (child.name.Contains("LOD"))
            {
                _lodTransform = child;
                break;
            }
        }

        if (_lodTransform != null)
        {
            Renderer[] renderers = _lodTransform.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                _originalMaterialsDict[renderer] = renderer.materials;
            }
        }
    }
    #endregion
}
