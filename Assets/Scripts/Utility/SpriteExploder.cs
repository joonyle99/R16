using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public struct FadeOption
{
    public bool UseFade;
    public float FadeDelay;
    public float FadeDuration;
    
    public static FadeOption None => new FadeOption 
    { 
        UseFade = false,
        FadeDelay = 0f,
        FadeDuration = 0f
    };
    
    public static FadeOption Default => new FadeOption 
    { 
        UseFade = true,
        FadeDelay = 5f,
        FadeDuration = 0.5f
    };
}

/// <summary>
/// 스프라이트를 N×N 조각으로 분할하여 폭발시키는 유틸리티
/// </summary>
public static class SpriteExploder
{
    public static void Explode(SpriteRenderer spriteRenderer, Vector3 position,
        int sliceCount = 4, float force = 50f, float gravityScale = 1f,
        bool hasCollider = true, FadeOption fadeOption = default)
    {
        var sprite = spriteRenderer.sprite;
        var texture = sprite.texture;
        var textureRect = sprite.textureRect;

        float spriteWidth = sprite.bounds.size.x;
        float spriteHeight = sprite.bounds.size.y;
        float pieceWidth = spriteWidth / sliceCount;
        float pieceHeight = spriteHeight / sliceCount;

        float texturePieceWidth = textureRect.width / sliceCount;
        float texturePieceHeight = textureRect.height / sliceCount;

        // 부모 오브젝트 생성
        var root = new GameObject("ExplodePieces");
        var pieces = new List<SpriteRenderer>();

        for (int x = 0; x < sliceCount; x++)
        {
            for (int y = 0; y < sliceCount; y++)
            {
                var pieceSprite = Sprite.Create(
                    texture,
                    new Rect(
                        textureRect.x + x * texturePieceWidth,
                        textureRect.y + y * texturePieceHeight,
                        texturePieceWidth,
                        texturePieceHeight
                    ),
                    new Vector2(0.5f, 0.5f),
                    sprite.pixelsPerUnit
                );

                var piece = new GameObject($"Piece_{x}_{y}");
                var spritePieceLayer = LayerMask.NameToLayer("SpritePiece");
                piece.layer = spritePieceLayer >= 0 ? spritePieceLayer : 0;
                piece.transform.SetParent(root.transform);
                piece.transform.position = new Vector2(
                    position.x + (x - sliceCount / 2f) * pieceWidth,
                    position.y + (y - sliceCount / 2f) * pieceHeight
                );

                var pieceSr = piece.AddComponent<SpriteRenderer>();
                pieceSr.sprite = pieceSprite;
                pieceSr.sortingLayerName = spriteRenderer.sortingLayerName;
                pieceSr.sortingOrder = spriteRenderer.sortingOrder;

                if (hasCollider)
                {
                    piece.AddComponent<BoxCollider2D>();
                }

                var rigidbody = piece.AddComponent<Rigidbody2D>();
                rigidbody.gravityScale = gravityScale;
                Vector2 dirVector = new Vector2(
                    Random.Range(-1f, 1f),
                    Random.Range(0.5f, 1f)
                ).normalized;
                rigidbody.AddForce(dirVector * force);
                rigidbody.AddTorque(Random.Range(-200f, 200f));

                pieces.Add(pieceSr);
            }
        }

        if (fadeOption.UseFade)
        {
            var runner = root.AddComponent<BatchFadeRunner>();
            runner.Initialize(pieces, fadeOption.FadeDelay, fadeOption.FadeDuration);
        }
    }

    /// <summary>
    /// 힘이 빠지며 부서지는 연출 — 약한 힘, 위로 떠오름, 페이드 아웃
    /// </summary>
    public static void Dissolve(SpriteRenderer spriteRenderer, Vector3 position,
        int sliceCount = 4, float force = 50f, float gravityScale = -0.1f,
        float fadeDelay = 0.3f, float fadeDuration = 0.6f)
    {
        var sprite = spriteRenderer.sprite;
        var texture = sprite.texture;
        var textureRect = sprite.textureRect;

        float spriteWidth = sprite.bounds.size.x;
        float spriteHeight = sprite.bounds.size.y;
        float pieceWidth = spriteWidth / sliceCount;
        float pieceHeight = spriteHeight / sliceCount;

        float texturePieceWidth = textureRect.width / sliceCount;
        float texturePieceHeight = textureRect.height / sliceCount;

        // 부모 오브젝트 생성
        var root = new GameObject("DissolvePieces");
        var pieces = new List<SpriteRenderer>();

        for (int x = 0; x < sliceCount; x++)
        {
            for (int y = 0; y < sliceCount; y++)
            {
                var pieceSprite = Sprite.Create(
                    texture,
                    new Rect(
                        textureRect.x + x * texturePieceWidth,
                        textureRect.y + y * texturePieceHeight,
                        texturePieceWidth,
                        texturePieceHeight
                    ),
                    new Vector2(0.5f, 0.5f),
                    sprite.pixelsPerUnit
                );

                var piece = new GameObject($"Piece_{x}_{y}");
                var spritePieceLayer = LayerMask.NameToLayer("SpritePiece");
                piece.layer = spritePieceLayer >= 0 ? spritePieceLayer : 0;
                piece.transform.SetParent(root.transform);
                piece.transform.position = new Vector2(
                    position.x + (x - sliceCount / 2f) * pieceWidth,
                    position.y + (y - sliceCount / 2f) * pieceHeight
                );

                var pieceSr = piece.AddComponent<SpriteRenderer>();
                pieceSr.sprite = pieceSprite;
                pieceSr.sortingLayerName = spriteRenderer.sortingLayerName;
                pieceSr.sortingOrder = spriteRenderer.sortingOrder + 1;

                var rigidbody = piece.AddComponent<Rigidbody2D>();
                rigidbody.gravityScale = gravityScale;
                Vector2 dir = new Vector2(
                    Random.Range(-0.5f, 0.5f),
                    Random.Range(0.3f, 1f)
                ).normalized;
                rigidbody.AddForce(dir * force);
                rigidbody.AddTorque(Random.Range(-50f, 50f));

                pieces.Add(pieceSr);
            }
        }

        var runner = root.AddComponent<BatchFadeRunner>();
        runner.Initialize(pieces, fadeDelay, fadeDuration);
    }

    /// <summary>
    /// 부모 오브젝트에 부착되어 자식 조각들을 일괄 페이드 아웃 후 파괴
    /// </summary>
    private class BatchFadeRunner : MonoBehaviour
    {
        private List<SpriteRenderer> _pieces;
        private float _fadeDelay;
        private float _fadeDuration;

        public void Initialize(List<SpriteRenderer> pieces, float fadeDelay, float fadeDuration)
        {
            _pieces = pieces;
            _fadeDelay = fadeDelay;
            _fadeDuration = fadeDuration;
            StartCoroutine(FadeAndDestroyAll());
        }

        private IEnumerator FadeAndDestroyAll()
        {
            yield return new WaitForSecondsRealtime(_fadeDelay);

            float elapsedTime = 0f;
            while (elapsedTime < _fadeDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / _fadeDuration);

                foreach (var sr in _pieces)
                {
                    if (sr == null) continue;
                    var color = sr.color;
                    color.a = alpha;
                    sr.color = color;
                }

                yield return null;
            }

            // 부모 파괴 → 자식 조각 전부 정리
            Destroy(gameObject);
        }
    }
}
