using UnityEngine;

public interface IIndicatorStrategy
{
    void Init(GameObject rootObject, PlayerController owner, string prefabName = null);
    void Activate();
    void UpdateStrategy(Vector3 mousePosition);
    void Deactivate();
    void SetVisible(bool isVisible);
}

public static class IndicatorStrategyFactory
{
    public static IIndicatorStrategy Create(string funcName)
    {
        switch (funcName)
        {
            case "AimAtMousePosition":
                return new AimStrategy();

            case "TrackMouseCursor":
                return new TrackMouseStrategy();

            case "ExpandScaleOverTime":
                return new ExpandStrategy();

            case "ObjectAimAtMousePosition":
                return new TheodoreSniperStrategy();

            default:
                return null;
        }
    }
}