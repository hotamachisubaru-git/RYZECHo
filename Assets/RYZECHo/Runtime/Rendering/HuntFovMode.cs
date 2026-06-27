namespace RYZECHo.Unity;

public enum HuntFovMode
{
    Standard100,
    Wide120,
    Sniper80,
    Custom,
}

public static class HuntFovModeExtensions
{
    public static float ToDegrees(this HuntFovMode mode, float customDegrees)
    {
        return mode switch
        {
            HuntFovMode.Standard100 => 100f,
            HuntFovMode.Wide120 => 120f,
            HuntFovMode.Sniper80 => 80f,
            _ => customDegrees,
        };
    }
}

