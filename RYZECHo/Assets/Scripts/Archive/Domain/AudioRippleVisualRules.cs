namespace RYZECHo;

internal readonly record struct AudioOcclusionProfile(
    int OccludingCells,
    float RangeMultiplier,
    float AlphaMultiplier,
    float BlurRadius);

internal readonly record struct FootstepCadenceResult(
    int NextPulseIndex,
    bool EmitsRipple,
    float RippleStrength,
    float CooldownSeconds);

internal readonly record struct RippleRingVisual(
    float Radius,
    float HaloRadius,
    Color RingColor,
    Color HaloColor,
    float StrokeWidth,
    float HaloStrokeWidth);

internal readonly record struct DirectionalCueVisual(
    PointF BodyStart,
    PointF BodyEnd,
    PointF HeadBase,
    PointF LeftWing,
    PointF RightWing,
    PointF ZigA,
    PointF ZigB,
    PointF Tip,
    Color BodyColor,
    Color GlowColor,
    float BodyWidth,
    float GlowWidth,
    bool UsesZigZag);

internal readonly record struct EdgeArrowVisual(
    PointF Tip,
    PointF Left,
    PointF Right,
    Color Color);

internal static class AudioRippleVisualRules
{
    private const float RangeAttenuationPerOccluder = 0.90f;
    private const float AlphaAttenuationPerOccluder = 0.72f;
    private const float HearingRangeScale = 1.8f;
    private const int FootstepsPerRipple = 3;

    public static AudioOcclusionProfile CalculateOcclusion(int occludingCells)
    {
        var clampedCells = Math.Max(0, occludingCells);
        return new AudioOcclusionProfile(
            clampedCells,
            MathF.Pow(RangeAttenuationPerOccluder, clampedCells),
            MathF.Pow(AlphaAttenuationPerOccluder, clampedCells),
            Math.Clamp(clampedCells * 3.2f, 0f, 14f));
    }

    public static float CalculateHearingRange(
        float actorHearingRange,
        float weaponHearingMultiplier,
        float rippleStrength,
        AudioOcclusionProfile occlusion)
    {
        return actorHearingRange * weaponHearingMultiplier * HearingRangeScale * Math.Max(0f, rippleStrength) * occlusion.RangeMultiplier;
    }

    public static float CalculateFootstepInterval(float movementSpeed)
    {
        var normalized = Math.Clamp((movementSpeed - 60f) / 170f, 0f, 1f);
        return 0.42f - (normalized * 0.16f);
    }

    public static FootstepCadenceResult AdvanceFootstepCadence(
        int currentPulseIndex,
        float movementSpeed,
        bool amplifiedSurface)
    {
        var nextPulseIndex = (Math.Clamp(currentPulseIndex, 0, FootstepsPerRipple - 1) + 1) % FootstepsPerRipple;
        return new FootstepCadenceResult(
            nextPulseIndex,
            nextPulseIndex == 0,
            amplifiedSurface ? 1.05f : 0.68f,
            CalculateFootstepInterval(movementSpeed));
    }

    public static RippleRingVisual CreateRingVisual(
        Ripple ripple,
        float progress,
        AudioOcclusionProfile occlusion,
        bool sharedOnly)
    {
        var clampedProgress = Math.Clamp(progress, 0f, 1f);
        var isBreathing = ripple.Kind == RippleKind.Breathing;
        var radius = isBreathing
            ? 10f + (clampedProgress * 44f * ripple.Strength)
            : 16f + (clampedProgress * 84f * ripple.Strength);
        var baseAlpha = isBreathing ? 86f : sharedOnly ? 92f : 150f;
        var alpha = (int)(baseAlpha * (1f - clampedProgress) * occlusion.AlphaMultiplier);
        var baseColor = sharedOnly ? Color.FromArgb(180, 124, 214, 255) : ripple.Color;
        var maxAlpha = isBreathing ? 112 : 165;
        var minAlpha = isBreathing ? 8 : 12;
        var ringColor = Color.FromArgb(Math.Clamp(alpha, minAlpha, maxAlpha), baseColor);
        var haloColor = Color.FromArgb(Math.Clamp(alpha / 2, 6, isBreathing ? 48 : 80), baseColor);

        return new RippleRingVisual(
            radius,
            radius + 8f + occlusion.BlurRadius,
            ringColor,
            haloColor,
            isBreathing ? 1.3f : 2f,
            Math.Clamp(1f + (occlusion.BlurRadius * 0.16f), 1f, 3.2f));
    }

    public static DirectionalCueVisual CreateDirectionalCue(
        PointF listenerPosition,
        Ripple ripple,
        float progress,
        AudioOcclusionProfile occlusion,
        bool sharedOnly)
    {
        var direction = Normalize(new PointF(ripple.Position.X - listenerPosition.X, ripple.Position.Y - listenerPosition.Y));
        var side = new PointF(-direction.Y, direction.X);
        var tail = 34f + (8f * ripple.Strength);
        var head = 18f + (6f * ripple.Strength);
        var wing = ripple.Kind == RippleKind.Skill ? 11f : 8f;
        var anchor = new PointF(listenerPosition.X + (direction.X * 54f), listenerPosition.Y + (direction.Y * 54f));
        var fade = Math.Clamp(1f - progress, 0f, 1f);
        var alpha = (int)(225f * fade * occlusion.AlphaMultiplier);
        var sourceColor = sharedOnly ? Color.FromArgb(180, 124, 214, 255) : ripple.Color;
        var bodyColor = Color.FromArgb(Math.Clamp(alpha, 18, 225), sourceColor);
        var glowColor = Color.FromArgb(Math.Clamp(alpha / 3, 8, 72), sourceColor);

        var bodyStart = new PointF(anchor.X - (direction.X * tail * 0.35f), anchor.Y - (direction.Y * tail * 0.35f));
        var bodyEnd = new PointF(anchor.X + (direction.X * tail), anchor.Y + (direction.Y * tail));
        var headBase = new PointF(bodyEnd.X - (direction.X * head), bodyEnd.Y - (direction.Y * head));
        var leftWing = new PointF(headBase.X + (side.X * wing), headBase.Y + (side.Y * wing));
        var rightWing = new PointF(headBase.X - (side.X * wing), headBase.Y - (side.Y * wing));
        var zigA = new PointF(headBase.X + (side.X * wing * 0.4f), headBase.Y + (side.Y * wing * 0.4f));
        var zigB = new PointF(headBase.X - (side.X * wing * 0.9f), headBase.Y - (side.Y * wing * 0.9f));
        var tip = new PointF(bodyEnd.X + (direction.X * 5f), bodyEnd.Y + (direction.Y * 5f));
        var blurWidth = occlusion.BlurRadius * 0.16f;

        return new DirectionalCueVisual(
            bodyStart,
            bodyEnd,
            headBase,
            leftWing,
            rightWing,
            zigA,
            zigB,
            tip,
            bodyColor,
            glowColor,
            ripple.Kind == RippleKind.Skill ? 3f : 3.4f,
            (ripple.Kind == RippleKind.Skill ? 5.2f : 5.8f) + blurWidth,
            ripple.Kind == RippleKind.Skill);
    }

    public static EdgeArrowVisual CreateEdgeArrow(
        PointF listenerPosition,
        PointF cameraCenter,
        Ripple ripple,
        bool sharedOnly)
    {
        var direction = Normalize(new PointF(ripple.Position.X - listenerPosition.X, ripple.Position.Y - listenerPosition.Y));
        var side = new PointF(-direction.Y, direction.X);
        var anchor = new PointF(cameraCenter.X + (direction.X * 220f), cameraCenter.Y + (direction.Y * 140f));
        var color = sharedOnly ? Color.FromArgb(210, 124, 214, 255) : Color.FromArgb(220, ripple.Color);
        return new EdgeArrowVisual(
            new PointF(anchor.X + (direction.X * 14f), anchor.Y + (direction.Y * 14f)),
            new PointF(anchor.X - (direction.X * 8f) + (side.X * 8f), anchor.Y - (direction.Y * 8f) + (side.Y * 8f)),
            new PointF(anchor.X - (direction.X * 8f) - (side.X * 8f), anchor.Y - (direction.Y * 8f) - (side.Y * 8f)),
            color);
    }

    private static PointF Normalize(PointF vector)
    {
        var length = MathF.Max(1f, MathF.Sqrt((vector.X * vector.X) + (vector.Y * vector.Y)));
        return new PointF(vector.X / length, vector.Y / length);
    }
}
