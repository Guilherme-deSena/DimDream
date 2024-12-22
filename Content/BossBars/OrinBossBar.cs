using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.GameContent;
namespace DimDream.Content.BossBars
{
    public class OrinBossBar : ModBossBar
    {
        private static Asset<Texture2D> SegmentSeparator { get; set; }

        public override void Load()
        {
            SegmentSeparator = Mod.Assets.Request<Texture2D>("Assets/Textures/SegmentSeparator");
        }

        public override bool PreDraw(SpriteBatch spriteBatch, NPC npc, ref BossBarDrawParams drawParams)
        {
            drawParams.ShowText = false;
            return true;
        }

        public override void PostDraw(SpriteBatch spriteBatch, NPC npc, BossBarDrawParams drawParams)
        {
            const int barWidth = 516;
            const int barHeight = 46;
            const int healthWidth = 456;

            string healthText = $"{npc.life}/{npc.lifeMax}";
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 textSize = font.MeasureString(healthText);
            Vector2 textPosition = drawParams.BarCenter;
                

            Vector2 healthStart = new(drawParams.BarCenter.X - barWidth / 2 + 32, drawParams.BarCenter.Y - barHeight / 2 + 12);
            Vector2 finalPosition = new(healthStart.X + healthWidth * .49f, healthStart.Y);

            spriteBatch.Draw(SegmentSeparator.Value, finalPosition, Color.White);
            spriteBatch.DrawString(font, healthText, textPosition, Color.White, 0f, textSize / 2f, 1f, SpriteEffects.None, 0f);
        }

        private void DrawText(SpriteBatch spriteBatch, Vector2 barPosition, int barWidth, int barHeight, float currentHealth, float stageStart, float stageEnd, Color color)
        {
            
        }


    }
}
