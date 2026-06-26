using Microsoft.Xna.Framework;

namespace RYZECHo.Audio
{
    /// <summary>
    /// ゲームモデルの視覚波紋を実音へ変換する薄いアダプター。
    /// 波紋の寿命や描画は GameModel 側が正とし、ここでは音色と再生間隔だけを扱います。
    /// </summary>
    internal sealed class AudioRippleSystem
    {
        private readonly AudioManager _audioManager;

        public AudioRippleSystem(AudioManager audioManager)
        {
            _audioManager = audioManager;
        }

        public void Play(RippleKind kind, float volume, float pan)
        {
            var profile = kind switch
            {
                RippleKind.Footstep => new RippleSoundProfile(SoundEffectCatalog.SFX_Ripple_Footstep, 0.34f, 0.055f),
                RippleKind.Breathing => new RippleSoundProfile(SoundEffectCatalog.SFX_Ripple_Breathing, 0.16f, 0.45f),
                RippleKind.Gunshot => new RippleSoundProfile(SoundEffectCatalog.SFX_Ripple_Gunshot, 0.72f, 0.035f),
                RippleKind.Skill => new RippleSoundProfile(SoundEffectCatalog.SFX_Ripple_Skill, 0.42f, 0.08f),
                _ => new RippleSoundProfile(SoundEffectCatalog.SFX_Ripple_Skill, 0.32f, 0.08f),
            };

            var effectiveVolume = MathHelper.Clamp(volume * profile.VolumeScale, 0.0f, 1.0f);
            if (effectiveVolume <= 0.015f)
            {
                return;
            }

            _audioManager.PlayEffect(
                profile.Key,
                effectiveVolume,
                pan: MathHelper.Clamp(pan, -1.0f, 1.0f),
                minIntervalSeconds: profile.MinIntervalSeconds);
        }

        private readonly record struct RippleSoundProfile(string Key, float VolumeScale, float MinIntervalSeconds);
    }
}
