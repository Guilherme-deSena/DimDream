using Terraria.Audio;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using DimDream.Content.NPCs;

namespace DimDream.Content.Items.Consumables
{
	internal class MiniatureHypervessel : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 3;
			ItemID.Sets.SortingPriorityBossSpawns[Type] = 12; // This helps sort inventory know that this is a boss summoning Item.

			// If this would be for a vanilla boss that has no summon item, you would have to include this line here:
			// NPCID.Sets.MPAllowedEnemies[NPCID.Plantera] = true;

			// Otherwise the UseItem code to spawn it will not work in multiplayer
		}

		public override void SetDefaults() {
			Item.width = 70;
			Item.height = 34;
			Item.maxStack = 9999;
			Item.value = 100;
			Item.rare = ItemRarityID.Blue;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.consumable = true;
		}

		public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup) {
			itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossSpawners;
		}

		public override bool CanUseItem(Player player) {
            // If you decide to use the below UseItem code, you have to include !NPC.AnyNPCs(id), as this is also the check the server does when receiving MessageID.SpawnBoss.
            // If you want more constraints for the summon item, combine them as boolean expressions:
            //    return !Main.dayTime && !NPC.AnyNPCs(ModContent.NPCType<ChiyuriBoss>()); would mean "not daytime and no ChiyuriBoss currently alive"
            return !NPC.AnyNPCs(ModContent.NPCType<ChiyuriBoss>());
		}

		public override bool? UseItem(Player player) {
			if (player.whoAmI == Main.myPlayer) {
				// If the player using the item is the client
				// (explicitly excluded serverside here)
				SoundEngine.PlaySound(SoundID.Roar, player.position);

				int type = ModContent.NPCType<ChiyuriBoss>();

				if (Main.netMode != NetmodeID.MultiplayerClient) {
					// If the player is not in multiplayer, spawn directly
					NPC.SpawnOnPlayer(player.whoAmI, type);
				}
				else {
					// If the player is in multiplayer, request a spawn
					// This will only work if NPCID.Sets.MPAllowedEnemies[type] is true, which is set in ChiyuriBoss
					NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);
				}
			}

			return true;
		}

        // ExampleMod contains a detailed explanation of recipe creation in Content/ExampleRecipes.cs
        public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient(ItemID.CobaltBar, 10)
                .AddIngredient(ItemID.SoulofLight, 5)
                .AddTile(TileID.Anvils)
				.Register();

            CreateRecipe()
                .AddIngredient(ItemID.PalladiumBar, 10)
                .AddIngredient(ItemID.SoulofLight, 5)
                .AddTile(TileID.Anvils)
                .Register();
        }
	}
}
