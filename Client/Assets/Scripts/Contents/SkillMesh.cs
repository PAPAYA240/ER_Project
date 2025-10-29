using System;
using System.Collections;
using Google.Protobuf.Protocol;
using UnityEngine;
#if UNITY_EDITOR
using static UnityEditor.PlayerSettings;
#endif

public class SkillMesh : MonoBehaviour
{
    public SkillHitbox _hitbox = new SkillHitbox();
    public GameObject visualObject;
    private Transform _playerTransform = null;
    public int segments = 32;

    // +추가
    public float ChargeRatio { get; set; } = 1;
    private LineRenderer lr = null;

    private Vector3 _mousePos = new Vector3();
    private Vector3 _pos;
    private Vector3 _forward;
    private Vector3 _right;
    private float _offsetRadius = 0f;

    public void Init(SkillHitbox hitbox, Transform playerTransform, int team = 0, float chargeRatio = 1, Vector3 mousePos = new Vector3()) // 투사체일 경우엔 투사체의 transform을 넣어줘야함
    {
        _hitbox = hitbox;
        _playerTransform = playerTransform;
        ChargeRatio = chargeRatio;
        _mousePos = mousePos;

        float startTime = (float)_hitbox.StartFrame / _hitbox.Fps;
        float endTime = (float)_hitbox.EndFrame / _hitbox.Fps;

        StartCoroutine(AutoDestroy(startTime, endTime, team));
    }

    private void CreateVisual(SkillShape shape, int team)
    {
        if (visualObject != null)
            Destroy(visualObject);

        visualObject = new GameObject(shape.ToString() + "Visual");
        visualObject.transform.SetParent(transform);

        var lr = visualObject.AddComponent<LineRenderer>();
        lr.startWidth = lr.endWidth = 0.05f;
        Material originalMat = Resources.Load<Material>("Prefabs/Debug/hitbox");
        lr.material = Instantiate(originalMat);

        UnityEngine.Color color;
        switch (team)
        {
            case 0:
                color = new UnityEngine.Color(1, 0.92f, 0.016f, 1);
                break;
            case 1:
                color = new UnityEngine.Color(1, 0, 1, 1);
                break;
            case 2:
                color = new UnityEngine.Color(0, 0, 1, 1);
                break;
            default:
                color = new UnityEngine.Color(0, 1, 0, 1);
                break;
        }
        lr.startColor = lr.endColor = color;
        lr.useWorldSpace = false;

        Enum.TryParse<SkillType>(_hitbox.Type, out SkillType type);
        if (type == SkillType.SkillTrack || type == SkillType.SkillProjectile)
        {
            transform.SetParent(_playerTransform);
            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localRotation = Quaternion.identity;
        }
        else if (type == SkillType.SkillPoint)
        {
            transform.SetParent(null);
            transform.position = _mousePos;
            transform.rotation = _playerTransform.rotation;

            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localRotation = Quaternion.identity;
            Debug.Log($"{transform.position}");
        }
        else
        {
            visualObject.transform.position = _playerTransform.position;
            visualObject.transform.rotation = _playerTransform.rotation;
        }

        switch (shape)
        {
            case SkillShape.Circle:
                visualObject.transform.localPosition = new Vector3(_hitbox.RightOffset, 0, _hitbox.LookOffset);
                DrawCircle(lr, _hitbox.Radius, 36);
                break;

            case SkillShape.Point:
                visualObject.transform.localPosition = Vector3.zero;
                DrawRectangle(lr, _hitbox.Width, _hitbox.Height);
                break;

            case SkillShape.Rectangle:
                visualObject.transform.localPosition = new Vector3(_hitbox.RightOffset, 0, _hitbox.LookOffset);
                DrawRectangle(lr, _hitbox.Width, _hitbox.Height);
                break;

            case SkillShape.Ray:
                float range = Mathf.Lerp(_hitbox.MinRange, _hitbox.MaxRange, ChargeRatio);
                visualObject.transform.localPosition = new Vector3(0, 0, range * 0.5f);
                DrawRectangle(lr, _hitbox.Width, range);
                break;

            case SkillShape.Sector:
                visualObject.transform.localPosition = new Vector3(_hitbox.RightOffset, 0, _hitbox.LookOffset);
                DrawSector(lr, _hitbox.Radius, _hitbox.Angle);
                break;

            case SkillShape.ShapeNone:
            default:
                Destroy(visualObject);
                visualObject = null;
                break;
        }
    }

    private void DrawCircle(LineRenderer lr, float radius, int segments)
    {
        lr.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * 2f * Mathf.PI / segments;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
        }
    }

    private void DrawRectangle(LineRenderer lr, float width, float height)
    {
        lr.positionCount = 5;
        Vector3[] points = new Vector3[5]
        {
        new Vector3(-width/2f, 0, -height/2f), // X, Y=0, Z
        new Vector3(-width/2f, 0, height/2f),
        new Vector3(width/2f, 0, height/2f),
        new Vector3(width/2f, 0, -height/2f),
        new Vector3(-width/2f, 0, -height/2f)
        };
        lr.SetPositions(points);
    }

    private void DrawSector(LineRenderer lr, float radius, float angleDegrees)
    {
        int segments = 20;
        lr.positionCount = segments + 3;

        lr.SetPosition(0, Vector3.zero); // 중심 (로컬)

        float startAngle = -angleDegrees / 2f;
        float step = angleDegrees / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (startAngle + step * i) * Mathf.Deg2Rad;

            // Z축(앞) 기준으로 반원 그리기
            Vector3 localPos = new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);

            lr.SetPosition(i + 1, localPos);
        }

        lr.SetPosition(segments + 2, Vector3.zero);
    }

    private IEnumerator AutoDestroy(float startTime, float endTime, int team)
    {
        yield return new WaitForSeconds(startTime);

        if (Enum.TryParse<SkillShape>(_hitbox.Shape, out SkillShape shape))
            CreateVisual(shape, team);

        float duration = endTime - startTime;
        if (duration > 0f)
            yield return new WaitForSeconds(duration);

        if (visualObject != null)
            Destroy(visualObject);

        Destroy(this.gameObject);
    }


    #region 추가
    //public void OnDraw(SkillHitbox hitbox, Vector3 pos, Vector3 forward, Vector3 right, float offsetRadius, int team)
    //{
    //    _pos = pos;
    //    _forward = forward;
    //    _right = right;
    //    _offsetRadius = offsetRadius;
    //    SetHitbox(hitbox, team);
    //}

    public void SetHitbox(SkillHitbox hitbox, int team, float chargeRatio = 1f)
    {
        _hitbox = hitbox;
        ChargeRatio = chargeRatio;

        if (visualObject != null)
            Destroy(visualObject);

        visualObject = new GameObject(hitbox.Shape.ToString() + "Visual");
        visualObject.transform.SetParent(transform);
        lr = visualObject.AddComponent<LineRenderer>();
        lr.startWidth = lr.endWidth = 0.05f;
        Material originalMat = Resources.Load<Material>("Prefabs/Debug/hitbox");
        lr.material = Instantiate(originalMat);

        UnityEngine.Color color;
        switch (team)
        {
            case 0:
                color = new UnityEngine.Color(1, 0.92f, 0.016f, 1);
                break;
            case 1:
                color = new UnityEngine.Color(1, 0, 1, 1);
                break;
            case 2:
                color = new UnityEngine.Color(0, 0, 1, 1);
                break;
            default:
                color = new UnityEngine.Color(0, 1, 0, 1);
                break;
        }
        lr.startColor = lr.endColor = color;
        lr.useWorldSpace = false;
        Enum.TryParse<SkillType>(_hitbox.Type, out SkillType type);

        visualObject.transform.position = _pos;
        visualObject.transform.forward = _forward.sqrMagnitude > 1e-6f ? _forward.normalized : Vector3.forward;

        StartCoroutine(AutoDestroy(0, 0.1f, 0));

        if (Enum.TryParse<SkillShape>(_hitbox.Shape, out SkillShape shape))
            Draw(shape);
    }
    public void Draw(SkillShape shape)
    {
        switch (shape)
        {
            case SkillShape.Circle:
                DebugCircle(lr, _hitbox.Radius + _offsetRadius, segments);
                break;

            case SkillShape.Rectangle:
            case SkillShape.Point:
                DebugRectangle(lr, _hitbox.Width, _hitbox.Height);
                break;

            case SkillShape.Ray:
                float range = Mathf.Lerp(_hitbox.MinRange, _hitbox.MaxRange, 1.0f);
                DebugRay(lr, _hitbox.Width, _hitbox.Height);
                break;

            case SkillShape.Sector:
                DebugSector(lr, _hitbox.Radius, _hitbox.Angle);
                break;
        }
    }

    void DebugRay(LineRenderer lr, float width, float range)
    {
        lr.positionCount = 5;

        Vector3 halfWidth = Vector3.right * width * 0.5f;
        Vector3 start = Vector3.zero;
        Vector3 end = Vector3.forward * range;

        lr.SetPosition(0, start - halfWidth);
        lr.SetPosition(1, start + halfWidth);
        lr.SetPosition(2, end + halfWidth);
        lr.SetPosition(3, end - halfWidth);
        lr.SetPosition(4, start - halfWidth); 
    }

    void DebugCircle(LineRenderer lr, float radius, int segments)
    {
        lr.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            Vector3 point = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            lr.SetPosition(i, point);
        }
    }
    void DebugRectangle(LineRenderer lr, float width, float height)
    {
        lr.positionCount = 5;

        Vector3 halfW = Vector3.right * width * 0.5f;
        Vector3 halfH = Vector3.forward * height * 0.5f;

        Vector3 center = Vector3.zero; // 로컬 중심

        lr.SetPosition(0, center - halfW - halfH);
        lr.SetPosition(1, center - halfW + halfH);
        lr.SetPosition(2, center + halfW + halfH);
        lr.SetPosition(3, center + halfW - halfH);
        lr.SetPosition(4, center - halfW - halfH);
    }

    private void DebugSector(LineRenderer lr, float radius, float angleDegrees)
    {
        int segments = 20;
        lr.positionCount = segments + 3;

        lr.SetPosition(0, Vector3.zero); 

        float startAngle = -angleDegrees / 2f;
        float step = angleDegrees / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (startAngle + step * i) * Mathf.Deg2Rad;
            Vector3 localPos = new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
            lr.SetPosition(i + 1, localPos);
        }
        lr.SetPosition(segments + 2, Vector3.zero);
    }

    #endregion

}

