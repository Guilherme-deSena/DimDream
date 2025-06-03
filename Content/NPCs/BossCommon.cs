using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;

namespace DimDream.Content.NPCs
{
    public static class BossCommon
    {
        public static void ArenaDust(Vector2 arenaCenter, int arenaRadius)
        {
            int dustCount = 5;
            for (int i = 0; i < dustCount; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 position = arenaCenter + new Vector2(0, -arenaRadius).RotatedBy(angle);
                float speed = 5f;
                Vector2 velocity = new Vector2(0, -speed).RotatedBy(angle + MathHelper.Pi / 2) * Main.rand.NextFloat(.1f, 1f);
                int type = DustID.BlueFairy;

                Dust.NewDustPerfect(position, type, velocity, 100, Color.Aqua, 1.5f);
            }
        }

        public static void PullPlayers(Vector2 arenaCenter, int pullDistance, int fightDistance)
        {

            foreach (Player player in Main.player)
            {
                float distance = Vector2.Distance(arenaCenter, player.Center);
                float pullStrength = Math.Clamp((distance - 800) / 50, 4f, 22f);
                bool isTooDistant = distance > pullDistance && distance < fightDistance;
                if (player.active && !player.dead && isTooDistant)
                {
                    Vector2 directionToArena = arenaCenter - player.Center;

                    directionToArena.Normalize();
                    directionToArena *= pullStrength;

                    player.velocity = directionToArena;
                }
            }
        }
    }
}
