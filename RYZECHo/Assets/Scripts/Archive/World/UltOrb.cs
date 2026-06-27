namespace RYZECHo.World
{
    /// <summary>
    /// マップ上に配置される ULT ポイント供給オブジェクト
    /// </summary>
    public class UltOrb
    {
        public int Id { get; set; }
        public System.Numerics.Vector2 Position { get; set; }
        public bool IsActive { get; set; } = true;
        public float CooldownSeconds { get; set; } = 0f;

        public void Update(float deltaTime)
        {
            if (!IsActive)
            {
                CooldownSeconds -= deltaTime;
                if (CooldownSeconds <= 0)
                {
                    IsActive = true;
                }
            }
        }
    }
}