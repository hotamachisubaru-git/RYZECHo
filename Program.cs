using System;

namespace RYZECHo.Prototype
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            // MonoGame プロジェクトの標準的なエントリポイント
            using var game = new Game1();
            game.Run();
        }
    }
}
