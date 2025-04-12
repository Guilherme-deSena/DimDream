using System;
using System.Collections.Generic;
using Terraria.ID;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.GameContent.Bestiary;
using DimDream.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace DimDream.Common.Systems
{
    public class ScrollingBackgroundSystem : ModSystem
    {/*
        public static bool BackgroundActive = false;
        private static Vector2 scrollOffset = Vector2.Zero;
        private static readonly float scrollSpeed = 1.5f;
        private static Texture2D bgTexture;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                bgTexture = ModContent.Request<Texture2D>("DimDream/Content/Backgrounds/Sky", AssetRequestMode.ImmediateLoad).Value;
            }
        }
        /*
        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            if (!BackgroundActive || Main.gameMenu || bgTexture == null)
                return;

            Rectangle screen = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);
            scrollOffset += new Vector2(scrollSpeed, scrollSpeed);

            // Wrap the offset
            scrollOffset.X %= bgTexture.Width;
            scrollOffset.Y %= bgTexture.Height;

            // Draw seamless tiling
            for (float x = -scrollOffset.X; x < Main.screenWidth; x += bgTexture.Width)
            {
                for (float y = -scrollOffset.Y; y < Main.screenHeight; y += bgTexture.Height)
                {
                    spriteBatch.Draw(bgTexture, new Vector2(x, y), Color.White);
                }
            }
        }*/
    }
}
