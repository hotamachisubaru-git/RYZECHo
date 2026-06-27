namespace RYZECHo;

public readonly record struct AgentProfile(
    AgentKind Kind,
    string Name,
    string Role,
    string SkillOne,
    string SkillTwo,
    string Ultimate,
    Color Accent,
    float SkillOneCooldown = 12f,
    float SkillTwoCooldown = 25f);

public static class AgentCatalog
{
    public static readonly AgentKind[] SelectionOrder =
    {
        AgentKind.Veil,
        AgentKind.Vine,
        AgentKind.Nitro,
        AgentKind.Oasis,
        AgentKind.Divide,
        AgentKind.Glitch,
    };

    public static AgentProfile Get(AgentKind kind)
    {
        return kind switch
        {
            AgentKind.Vine => new(
                kind,
                "ヴァイン",
                "偵察兵",
                "ソナー矢",
                "サイレンス",
                "ハンターズアイ",
                Color.FromArgb(255, 124, 228, 255),
                SkillOneCooldown: 18f,
                SkillTwoCooldown: 30f),
            AgentKind.Nitro => new(
                kind,
                "ニトロ",
                "アタッカー",
                "瞬間加速",
                "インパクト",
                "オーバードライブ",
                Color.FromArgb(255, 255, 132, 92),
                SkillOneCooldown: 10f,
                SkillTwoCooldown: 22f),
            AgentKind.Oasis => new(
                kind,
                "オアシス",
                "支援",
                "ナノスモーク",
                "再生ナノ",
                "オーバーシールド",
                Color.FromArgb(255, 116, 232, 172),
                SkillOneCooldown: 15f,
                SkillTwoCooldown: 35f),
            AgentKind.Divide => new(
                kind,
                "ディバイド",
                "エンジニア",
                "拘束トラップ",
                "警告センサー",
                "ロックダウン",
                Color.FromArgb(255, 230, 194, 88),
                SkillOneCooldown: 20f,
                SkillTwoCooldown: 40f),
            AgentKind.Glitch => new(
                kind,
                "グリッチ",
                "撹乱",
                "索敵ペット",
                "ゴースト",
                "システムクラッシュ",
                Color.FromArgb(255, 196, 132, 255),
                SkillOneCooldown: 14f,
                SkillTwoCooldown: 28f),
            _ => new(
                AgentKind.Veil,
                "ヴェール",
                "科学医",
                "毒霧弾",
                "防弾壁",
                "致死ドーム",
                Color.FromArgb(255, 164, 220, 116),
                SkillOneCooldown: 12f,
                SkillTwoCooldown: 25f),
        };
    }
}
