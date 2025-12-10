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
    private List<Transform> _renderTargets = new List<Transform>();
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
        foreach (Transform target in _renderTargets)
        {
            if (target == null) continue;
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = false;
            }
        }
    }

    // 렌더러 활성화
    public void MakeVisible()
    {
        if (_mode == HighlightMode.None)
            return;

        _mode = HighlightMode.None;
        foreach (Transform target in _renderTargets)
        {
            if (target == null) continue;
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = true;
                if (_originalMaterialsDict.TryGetValue(renderer, out Material[] originalMaterials))
                {
                    renderer.materials = originalMaterials;
                }
            }
        }
    }

    public void ChangeBushRenderer()
    {
        if (_mode == HighlightMode.Bush)
            return;

        _mode = HighlightMode.Bush;
        foreach (Transform target in _renderTargets)
        {
            if (target == null) continue;
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer.gameObject.GetComponent<ParticleSystem>() != null)
                    continue;

                if (renderer.gameObject.GetComponent<TrailRenderer>() != null)
                    continue;

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
    }
    #endregion

    private bool CheckCondition()
    {
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
            if (child.name.Contains("LOD") || child.name.Contains("Rest"))
            {
                _renderTargets.Add(child);
            }
        }

        foreach (Transform target in _renderTargets)
        {
            if (target != null)
            {
                Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    _originalMaterialsDict[renderer] = renderer.materials;
                }
            }
        }
    }
    #endregion
}
