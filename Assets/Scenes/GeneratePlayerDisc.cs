using UnityEngine;
using System;

[ExecuteInEditMode]
public class GeneratePlayerDisc : MonoBehaviour
{
    void Start()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "PlayerDisc";
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var center = (size - 1) * 0.5f;
        var radius = size * 0.42f;
        var pixels = new Color32[size * size];

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var dist = Mathf.Sqrt(dx * dx + dy * dy);
                var alpha = Mathf.Clamp01(radius + 1.5f - dist);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, true);

        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = sprite;
            sr.color = new Color(0.48f, 0.9f, 1f, 1f);
            sr.sortingOrder = 50;
        }

        Debug.Log("PlayerDisc generated successfully");
    }
}