using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Terraria.Localization;

namespace DimDream.Content
{
    public class RecipeGroups : ModSystem
    {
        // A place to store the recipe group so we can easily use it later
        public static RecipeGroup Group;

        public override void Unload()
        {
            Group = null;
        }

        public override void AddRecipeGroups()
        {
            // Create a recipe group and store it
            // Language.GetTextValue("LegacyMisc.37") is the word "Any" in English, and the corresponding word in other languages
            Group = new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.Tombstone)}",
                ItemID.Tombstone, ItemID.GraveMarker, ItemID.CrossGraveMarker, ItemID.Headstone, ItemID.Gravestone, ItemID.Obelisk,
                ItemID.RichGravestone1, ItemID.RichGravestone2, ItemID.RichGravestone3, ItemID.RichGravestone4, ItemID.RichGravestone5);

            RecipeGroup.RegisterGroup(nameof(ItemID.Tombstone), Group);


            Group = new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.RottenChunk)}",
                ItemID.RottenChunk, ItemID.Vertebrae);

            RecipeGroup.RegisterGroup(nameof(ItemID.RottenChunk), Group);
        }
    }
}
