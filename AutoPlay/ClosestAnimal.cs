using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Network;
using StardewValley.TerrainFeatures;
using System.Threading;
using StardewValley.Menus;
using StardewValley.Objects;
using Microsoft.Xna.Framework;


namespace AutoPlaySV
{
    class ClosestAnimal
    {
        public FarmAnimal InnerAnimal { get; set; }

        public ClosestAnimal(FarmAnimal farmAnimal)
        {
            this.InnerAnimal = farmAnimal;
        }

        public int SortedPosition()
        {
            return (int)(this.InnerAnimal.position.X / 64 * 100 + this.InnerAnimal.position.Y / 64);
        }

        public FarmAnimal GetAnimal()
        {
            return this.InnerAnimal;
        }
    }
}
