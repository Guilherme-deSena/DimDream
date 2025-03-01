using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using System.Collections;
namespace DimDream.Content.BossBars
{
    public class OrinCatBossBar : ModBossBar
    {
        public int bossHeadIndex = -1;
        public static Asset<Texture2D> SegmentSeparator { get; set; }
        public static Asset<Texture2D> Star { get; set; }
        public int LastHealthBar { get; set; } = 1;
        
        public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame)
        {
            // Display the previously assigned head index
            if (bossHeadIndex != -1)
            {
                return TextureAssets.NpcHeadBoss[bossHeadIndex];
            }
            return null;
        }
        public override bool? ModifyInfo(ref BigProgressBarInfo info, ref float life, ref float lifeMax, ref float shield, ref float shieldMax)
        {
            NPC npc = Main.npc[info.npcIndexToAimAt];
            if (!npc.active)
                return false;

            bossHeadIndex = npc.GetBossHeadTextureIndex();
            return null;
        }
        public override void Load()
        {
            SegmentSeparator = Mod.Assets.Request<Texture2D>("Assets/Textures/SegmentSeparator");
            Star = Mod.Assets.Request<Texture2D>("Assets/Textures/Star");
        }

        public override bool PreDraw(SpriteBatch spriteBatch, NPC npc, ref BossBarDrawParams drawParams)
        {
            drawParams.ShowText = false; // Text will have to be manually drawn in order to be over the segment separators
            return true;
        }

        public override void PostDraw(SpriteBatch spriteBatch, NPC npc, BossBarDrawParams drawParams)
        {
            int barWidth = 516;
            int barHeight = 46;
            int healthWidth = 446;
            int stageHelper = (int)npc.localAI[2];
            int currentHealthBar = stageHelper / 10;
            Vector2 barStart = new(drawParams.BarCenter.X - barWidth / 2, drawParams.BarCenter.Y - barHeight / 2);
            Vector2 healthStart = new(barStart.X + 32, barStart.Y + 12);

            DrawSegments(spriteBatch, healthStart, currentHealthBar, healthWidth);

            // Draw health text
            string healthText = $"{npc.life}/{npc.lifeMax}";
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 textSize = font.MeasureString(healthText);
            Vector2 textPosition = drawParams.BarCenter;
            spriteBatch.DrawString(font, healthText, textPosition, Color.White, 0f, textSize / 2f, 1f, SpriteEffects.None, 0f);

            // Draw stars indicating how many healthbars are left
            for (int i = currentHealthBar; i < LastHealthBar; i++)
            {
                Vector2 position = new(barStart.X + 10 + (20 * i), barStart.Y - 20);
                spriteBatch.Draw(Star.Value, position, Color.White);
            }
        }

        public virtual void DrawSegments(SpriteBatch spriteBatch, Vector2 healthStart, int currentHealthBar, int healthWidth)
        {
            if (currentHealthBar == 1)
            {
                Vector2 finalPosition = new(healthStart.X + healthWidth * .8f, healthStart.Y);
                spriteBatch.Draw(SegmentSeparator.Value, finalPosition, Color.White);

                finalPosition.X = healthStart.X + healthWidth * .5f;
                spriteBatch.Draw(SegmentSeparator.Value, finalPosition, Color.White);
            }
        }
    }
    public class OrinHumanoidBossBar : OrinCatBossBar
    {
        public override void SetStaticDefaults()
        {
            LastHealthBar = 3;
        }

        public override void DrawSegments(SpriteBatch spriteBatch, Vector2 healthStart, int currentHealthBar, int healthWidth)
        {
            Vector2 finalPosition;
            switch (currentHealthBar)
            {
                case 0:
                    finalPosition = new(healthStart.X + healthWidth * .6f, healthStart.Y);
                    spriteBatch.Draw(SegmentSeparator.Value, finalPosition, Color.White);
                    break;

                case 1:
                    finalPosition = new(healthStart.X + healthWidth * .6f, healthStart.Y);
                    spriteBatch.Draw(SegmentSeparator.Value, finalPosition, Color.White);
                    break;

                case 2:
                    finalPosition = new(healthStart.X + healthWidth * .55f, healthStart.Y);
                    spriteBatch.Draw(SegmentSeparator.Value, finalPosition, Color.White);
                    break;
            }
        }
    }
}
