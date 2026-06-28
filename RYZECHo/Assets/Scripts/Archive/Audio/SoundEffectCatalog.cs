namespace RYZECHo.Audio
{
    /// <summary>
    /// ゲーム内の効果音パスを一元管理するカタログ。
    /// </summary>
    public static class SoundEffectCatalog
    {
        // BGM
        public const string BGM_HoloTheme = "BGM/prototype_holo_theme";

        // SFX - UI
        public const string SFX_UI_Confirm = "SFX/UI/confirm_beep";

        // SFX - Impact
        public const string SFX_Impact_Hit = "SFX/Impact/hit_ping";

        // SFX - Weapon
        public const string SFX_Weapon_Rifle = "SFX/Weapon/rifle_fire";

        // SFX - Footstep
        public const string SFX_Footstep_Hard = "SFX/Footstep/hard_step";

        // SFX - Audio Ripple (音の波紋)
        public const string SFX_Ripple_Footstep = "SFX/Footstep/hard_step";
        public const string SFX_Ripple_Breathing = "SFX/Footstep/hard_step";
        public const string SFX_Ripple_Gunshot = "SFX/Weapon/rifle_fire";
        public const string SFX_Ripple_Skill = "SFX/UI/confirm_beep";

        public static readonly string[] All =
        [
            BGM_HoloTheme,
            SFX_UI_Confirm,
            SFX_Impact_Hit,
            SFX_Weapon_Rifle,
            SFX_Footstep_Hard,
        ];
    }
}
