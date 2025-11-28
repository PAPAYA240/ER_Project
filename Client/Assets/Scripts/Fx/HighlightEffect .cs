using Google.Protobuf.Protocol;
using System;
using System.Linq;
using UnityEngine;

// 이거 쓰려면 스키닝 메시 + 아웃라인 머터리얼 + 충돌 캡슐 필요
public class HighlightEffect : MonoBehaviour
{
    private Renderer[] myRenderers;
    private Material outlineMaterial;
    private Material[][] originalMaterials;

    // 커서
    private Texture2D _cursorDefault;
    private Texture2D _cursorEnemy;
    private bool isHighlighted = false;

    public CreatureController Owner { get; set; }
    void Start()
    {
        _cursorDefault = Managers.Resource.Load<Texture2D>("Cursor/Cursor_01");
        _cursorEnemy = Managers.Resource.Load<Texture2D>("Cursor/Cursor_05");

        myRenderers = GetComponentsInChildren<Renderer>();
        outlineMaterial = Resources.Load<Material>("materials/Outline/Outline");

        if (outlineMaterial == null || myRenderers == null || myRenderers.Length == 0)
        {
            Debug.LogError("아웃라인 머티리얼 또는 렌더러가 없습니다.");
            return;
        }

        originalMaterials = new Material[myRenderers.Length][];
        for (int i = 0; i < myRenderers.Length; i++)
            originalMaterials[i] = myRenderers[i].sharedMaterials;
        outlineMaterial = new Material(Shader.Find("Custom/Outline_Shader"));
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool hitThisObject = false;

        RaycastHit[] hits = Physics.RaycastAll(ray, 100.0f);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            GameObject go = hit.collider.gameObject;
            if (go == null)
                continue;
            if (go != this.gameObject)
                continue;
            CreatureController cc = go.GetComponentInChildren<CreatureController>();
            if (cc == null)
                continue;
            if (cc.Untargetable)
                continue;

            hitThisObject = true;
        }

        if (Owner?.State == CreatureState.Dead)
        {
            OnMouseExit();
            return;
        }

        if (hitThisObject && !isHighlighted)
            OnMouseEnter();
        else if (!hitThisObject && isHighlighted)
            OnMouseExit();
    }

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
       
        isHighlighted = true;
        foreach (var renderer in myRenderers)
        {
            if (renderer == null) continue;
            Material[] newMaterials = renderer.materials;
            renderer.materials = newMaterials.Append(outlineMaterial).ToArray();
        }
        Cursor.SetCursor(_cursorEnemy, Vector2.zero, CursorMode.Auto);
    }

    public void OnMouseExit()
    {
        if (myRenderers == null) 
            return;

        isHighlighted = false;
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
    void OnDestroy()
    {
        if (outlineMaterial != null)
            Destroy(outlineMaterial);
    }
}
