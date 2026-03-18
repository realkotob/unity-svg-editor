using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor.Shared
{
    internal readonly struct PreviewImageSource
    {
        private PreviewImageSource(Sprite sprite, Texture texture, VectorImage vectorImage)
        {
            Sprite = sprite;
            Texture = texture;
            VectorImage = vectorImage;
        }

        public Sprite Sprite { get; }
        public Texture Texture { get; }
        public VectorImage VectorImage { get; }

        public bool IsEmpty => Sprite == null && Texture == null && VectorImage == null;

        public static PreviewImageSource None => default;

        public static PreviewImageSource FromSprite(Sprite sprite)
        {
            return new PreviewImageSource(sprite, null, null);
        }

        public static PreviewImageSource FromTexture(Texture texture)
        {
            return new PreviewImageSource(null, texture, null);
        }

        public static PreviewImageSource FromVectorImage(VectorImage vectorImage)
        {
            return new PreviewImageSource(null, null, vectorImage);
        }
    }

    internal static class PreviewImageHelper
    {
        public static void Apply(Image element, PreviewImageSource source)
        {
            if (element == null)
            {
                return;
            }

            element.vectorImage = source.VectorImage;
            element.sprite = source.Sprite;
            element.image = source.Texture;
        }

        public static void Apply(VisualElement element, PreviewImageSource source)
        {
            if (element == null)
            {
                return;
            }

            if (source.VectorImage != null)
            {
                element.style.backgroundImage = new StyleBackground(source.VectorImage);
                return;
            }

            if (source.Sprite != null)
            {
                element.style.backgroundImage = new StyleBackground(source.Sprite);
                return;
            }

            if (source.Texture is RenderTexture renderTexture)
            {
                element.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(renderTexture));
                return;
            }

            if (source.Texture is Texture2D texture2D)
            {
                element.style.backgroundImage = new StyleBackground(texture2D);
                return;
            }

            element.style.backgroundImage = StyleKeyword.None;
        }

        public static void Clear(Image element)
        {
            if (element == null)
            {
                return;
            }

            element.vectorImage = null;
            element.sprite = null;
            element.image = null;
        }

        public static void Clear(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            element.style.backgroundImage = StyleKeyword.None;
        }
    }
}
