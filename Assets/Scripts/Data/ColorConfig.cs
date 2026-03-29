using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorConfig", menuName = "BusJam/Color Config")]
public class ColorConfig : ScriptableObject
{
    public ColorMapping[] colorMappings;

    public Color GetRenderColor(StickmanColor stickmanColor)
    {
        for (int i = 0; i < colorMappings.Length; i++)
        {
            if (colorMappings[i].stickmanColor == stickmanColor)
                return colorMappings[i].renderColor;
        }
        return Color.white;
    }
}

[Serializable]
public struct ColorMapping
{
    public StickmanColor stickmanColor;
    public Color renderColor;
}
