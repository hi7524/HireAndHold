using UnityEngine;

public class LobbyWorldInitializer : MonoBehaviour
{
    public Sprite backgroundSprite;

    void Start()
    {
        GameObject bg = new GameObject("WorldBackground");
        SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = backgroundSprite;

        sr.sortingLayerName = "LobbyWorld";
        sr.sortingOrder = 0;

        Camera cam = Camera.main;

        float texW = backgroundSprite.texture.width;
        float texH = backgroundSprite.texture.height;

        float ppu = backgroundSprite.pixelsPerUnit;

        float spriteW = texW / ppu;
        float spriteH = texH / ppu;

        float worldH = cam.orthographicSize * 2f;
        float worldW = worldH * cam.aspect;

        float scaleX = worldW / spriteW;
        float scaleY = worldH / spriteH;

        float finalScale = Mathf.Max(scaleX, scaleY);

        bg.transform.localScale = Vector3.one * finalScale;

        Vector3 pos = cam.transform.position;
        pos.z = 0f;
        bg.transform.position = pos;
    }
}
