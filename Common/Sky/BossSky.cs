using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using Terraria;
using Terraria.GameContent.UI.States;

namespace DimDream.Common.Sky
{
    public class BossSky : CustomSky
    {
        private static Vector2 scrollSpeed = new(.5f, .5f);
        private static Texture2D texture;
        private static Texture2D texture2;
        private float rotationAngle = 0f; 
        private float fadeOpacity = 0f;
        private bool isActive = false;
        private Vector2 offset;
        public override void OnLoad()
        {
            if (!Main.dedServ)
            {
                texture = ModContent.Request<Texture2D>("DimDream/Assets/Textures/FieryBackground", AssetRequestMode.ImmediateLoad).Value;
                texture2 = ModContent.Request<Texture2D>("DimDream/Assets/Textures/Spiral", AssetRequestMode.ImmediateLoad).Value;
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (isActive)
            {
                isActive = false;
                fadeOpacity = MathHelper.Clamp(fadeOpacity + 0.02f, 0f, 1f);
                offset += scrollSpeed;
                rotationAngle += .002f;
            } else
            {
                fadeOpacity = MathHelper.Clamp(fadeOpacity - 0.02f, 0f, 1f);
            }
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (Main.gameMenu || texture == null)
                return;

            int texWidth = texture.Width;
            int texHeight = texture.Height;

            float xOffset = offset.X % texWidth;
            float yOffset = offset.Y % texHeight;

            int tilesX = Main.screenWidth / texWidth + 2;
            int tilesY = Main.screenHeight / texHeight + 2;


            for (int x = -1; x < tilesX; x++)
            {
                for (int y = -1; y < tilesY; y++)
                {
                    Vector2 drawPos = new Vector2(x * texWidth - xOffset, y * texHeight - yOffset);
                    spriteBatch.Draw(texture, drawPos, Color.White * fadeOpacity);
                }
            }


            spriteBatch.Draw(
                texture2, 
                new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f), 
                null, 
                Color.White * .2f * fadeOpacity, 
                rotationAngle, 
                texture2.Size() / 2f,
                1f, 
                SpriteEffects.None,
                0f
            );
        }

        public override float GetCloudAlpha() => 0f;
        public override bool IsActive() => fadeOpacity > 0f;
        public override bool IsVisible() => fadeOpacity > 0f;

        public override void Reset()
        {
            isActive = false;
            offset = Vector2.Zero;
        }

        public override void Activate(Vector2 position, params object[] args)
        {
            isActive = true;
        }

        public override void Deactivate(params object[] args)
        {
            isActive = false;
        }
    }
}
