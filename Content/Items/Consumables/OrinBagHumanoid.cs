using DimDream.Content.NPCs;
using DimDream.Content.Items.Weapons;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using DimDream.Content.Projectiles;
using DimDream.Content.Items.Accessories;

namespace DimDream.Content.Items.Consumables
{
    public class OrinBagHumanoid : ModItem
    {
        public override void SetStaticDefaults()
        {
            // This set is one that every boss bag should have.
            // It will create a glowing effect around the item when dropped in the world.
            // It will also let our boss bag drop dev armor.
            ItemID.Sets.BossBag[Type] = true;
            // ItemID.Sets.PreHardmodeLikeBossBag[Type] = true; // This should only be set for pre-hardmode bosses

            Item.ResearchUnlockCount = 3;
        }

        public override void SetDefaults()
        {
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.width = 24;
            Item.height = 24;
            Item.rare = ItemRarityID.Purple;
            Item.expert = true; // This makes sure that "Expert" displays in the tooltip and the item name color changes
        }

        public override bool CanRightClick()
        {
            return true;
        }

        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.OneFromOptions(1, [ModContent.ItemType<Phantasmagoria>(), ModContent.ItemType<ZombieFairyStaff>(), ModContent.ItemType<KashasPaw>(), ModContent.ItemType<CursedStick>()]));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<OrinsCartEnabled>(), 4));
            itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<OrinBossHumanoid>()));
        }
    }
}