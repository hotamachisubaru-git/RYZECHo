using UnityEngine;

namespace RYZECHo
{
    /// <summary>
    /// オーディオ関連のボリューム値をScriptableObjectに外部化。
    /// AudioMixerのGroup/Snapshot設定と連携して、インスペクタから音量調整を可能にする。
    /// </summary>
    [CreateAssetMenu(fileName = "AudioSettings", menuName = "RYZECHo/Settings/Audio Settings")]
    public sealed class AudioSettingsSO : ScriptableObject
    {
        #region Master Volume

        [Header("Master Volume")]
        [Tooltip("マスターボリューム（0〜1）")]
        [Range(0f, 1f)]
        public float MasterVolume = 1f;

        #endregion

        #region Music Volume

        [Header("Music Volume")]
        [Tooltip("音楽ボリューム（0〜1）")]
        [Range(0f, 1f)]
        public float MusicVolume = 1f;

        [Tooltip("BGMフェードイン時間（秒）")]
        public float MusicFadeInSeconds = 1.5f;

        [Tooltip("BGMフェードアウト時間（秒）")]
        public float MusicFadeOutSeconds = 2f;

        #endregion

        #region SFX Volume

        [Header("SFX Volume")]
        [Tooltip("効果音ボリューム（0〜1）")]
        [Range(0f, 1f)]
        public float SfxVolume = 1f;

        [Tooltip("足音ボリューム倍率")]
        public float FootstepVolumeMultiplier = 1f;

        [Tooltip("リップルエフェクト音量（dB）")]
        public float RippleVolumeDb = -6f;

        [Tooltip("リップル減衰距離（m）")]
        public float RippleAttenuationDistance = 25f;

        #endregion

        #region Voice Volume

        [Header("Voice Volume")]
        [Tooltip("ボイスボリューム（0〜1）")]
        [Range(0f, 1f)]
        public float VoiceVolume = 1f;

        [Tooltip("ボイスフェード時間（秒）")]
        public float VoiceFadeSeconds = 0.5f;

        #endregion

        #region Environment Volume

        [Header("Environment Volume")]
        [Tooltip("環境音ボリューム（0〜1）")]
        [Range(0f, 1f)]
        public float EnvironmentVolume = 0.8f;

        [Tooltip("環境音ランダムピッチ（最小）")]
        public float EnvironmentPitchMin = 0.95f;

        [Tooltip("環境音ランダムピッチ（最大）")]
        public float EnvironmentPitchMax = 1.05f;

        #endregion

        #region AudioMixer References

        [Header("Mixer References")]
        [Tooltip("マスターAudioMixer")]
        public AudioMixer MasterMixer;

        [Tooltip("BGM用AudioMixerGroup")]
        public AudioMixerGroup MusicGroup;

        [Tooltip("効果音用AudioMixerGroup")]
        public AudioMixerGroup SfxGroup;

        [Tooltip("ボイス用AudioMixerGroup")]
        public AudioMixerGroup VoiceGroup;

        [Tooltip("環境音用AudioMixerGroup")]
        public AudioMixerGroup EnvironmentGroup;

        #endregion
    }
}
