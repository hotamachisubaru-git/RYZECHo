using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace RYZECHo.UI;

internal static class UiImageExtensions
{
    public static void SetColor(this Image image, Color color)
    {
        if (image != null)
        {
            image.color = color;
        }
    }
}
