namespace RYZECHo;

/// <summary>
/// GameModelの生成を抽象化するファクトリインターフェース。
/// 依存性注入によりテスト容易性とモジュール間の結合度を低下させる。
/// </summary>
internal interface IGameModelFactory
{
    /// <summary>新しいGameModelインスタンスを生成する。</summary>
    GameModel Create();

    /// <summary>SO引数付きで新しいGameModelインスタンスを生成する。</summary>
    GameModel Create(IEventBus? eventBus, GameRulesSettingsSO gameRules, LayoutSettingsSO layoutSettings, GameplaySettingsSO gameplaySettings, VisualSettingsSO visualSettings, AudioSettingsSO audioSettings);

    /// <summary>ScriptableObject引数付きで新しいGameModelインスタンスを生成する（null安全）。</summary>
    GameModel Create(GameRulesSettingsSO? gameRules, LayoutSettingsSO? layoutSettings, GameplaySettingsSO? gameplaySettings);
}

/// <summary>
/// GameModelのデフォルト実装ファクトリ。
/// </summary>
internal sealed class GameModelFactory : IGameModelFactory
{
    /// <summary>GameModelFactory の singleton 参照（Unity 側での後方互換用）。</summary>
    public static readonly GameModelFactory Instance = new();

    public GameModel Create() => new();

    public GameModel Create(GameRulesSettingsSO? gameRules, LayoutSettingsSO? layoutSettings, GameplaySettingsSO? gameplaySettings)
    {
        return new(
            gameRules: gameRules,
            layoutSettings: layoutSettings,
            gameplaySettings: gameplaySettings);
    }

    public GameModel Create(
        IEventBus? eventBus,
        GameRulesSettingsSO gameRules,
        LayoutSettingsSO layoutSettings,
        GameplaySettingsSO gameplaySettings,
        VisualSettingsSO visualSettings,
        AudioSettingsSO audioSettings)
    {
        return new(
            eventBus: eventBus,
            gameRules: gameRules ?? UnityEngine.ScriptableObject.CreateInstance<GameRulesSettingsSO>(),
            layoutSettings: layoutSettings ?? UnityEngine.ScriptableObject.CreateInstance<LayoutSettingsSO>(),
            gameplaySettings: gameplaySettings ?? UnityEngine.ScriptableObject.CreateInstance<GameplaySettingsSO>(),
            visualSettings: visualSettings ?? UnityEngine.ScriptableObject.CreateInstance<VisualSettingsSO>(),
            audioSettings: audioSettings ?? UnityEngine.ScriptableObject.CreateInstance<AudioSettingsSO>());
    }
}
