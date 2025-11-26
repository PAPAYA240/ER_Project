using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Util
{
    public static GameObject FindChildByName(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child.gameObject;
            }

            GameObject found = FindChildByName(child, childName);

            if (found != null)
                return found;
        }
        return null;
    }

    public static T GetOrAddComponent<T>(GameObject go) where T : UnityEngine.Component
    {
        T component = go.GetComponent<T>();
		if (component == null)
            component = go.AddComponent<T>();
        return component;
	}

    public static GameObject FindChild(GameObject go, string name = null, bool recursive = false)
    {
        Transform transform = FindChild<Transform>(go, name, recursive);
        if (transform == null)
            return null;
        
        return transform.gameObject;
    }

    public static T FindChild<T>(GameObject go, string name = null, bool recursive = false) where T : UnityEngine.Object
    {
        if (go == null)
            return null;

        if (recursive == false)
        {
            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform transform = go.transform.GetChild(i);
                if (string.IsNullOrEmpty(name) || transform.name == name)
                {
                    T component = transform.GetComponent<T>();
                    if (component != null)
                        return component;
                }
            }
		}
        else
        {
            foreach (T component in go.GetComponentsInChildren<T>())
            {
                if (string.IsNullOrEmpty(name) || component.name == name)
                    return component;
            }
        }

        return null;
    }

    // Image Slice
    public static Sprite[] Slice(Texture2D spriteSheet, int columns, int rows, float padding)
    {
        if (spriteSheet == null)
        {
            Debug.LogError("No sprite sheet");
            return null;
        }

        int totalWidth = spriteSheet.width;
        int totalHeight = spriteSheet.height;

        // 각 스프라이트의 크기 계산 (패딩 포함)
        float spriteWidth = (totalWidth - (columns - 1) * padding) / columns;
        float spriteHeight = (totalHeight - (rows - 1) * padding) / rows;

        List<Sprite> frames = new List<Sprite>();

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                float posX = x * (spriteWidth + padding);
                float posY = (rows - 1 - y) * (spriteWidth + padding);

                Rect rect = new Rect(posX, posY, spriteWidth, spriteHeight);

                Sprite sprite = Sprite.Create(spriteSheet, rect, new Vector2(0.5f, 0.5f), 100f);
                frames.Add(sprite);
            }
        }

        return frames.ToArray();
    }
}
