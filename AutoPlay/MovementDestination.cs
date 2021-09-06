using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Network;

namespace AutoPlaySV
{
    class MovementDestination
    {
        public enum MovementAction
        {
            EnterAnimalBuilding,
            EnterFarmHouse,
            EnterGreenhouse
        }

        public Point Destination { get; set; }
        
        public MovementAction MovAction { get; set; }

        public ModEntry.NextAction NextAction { get; set; }


        public MovementDestination(Point destination, MovementAction movementAction, ModEntry.NextAction nextAction)
        {
            this.Destination = destination;
            this.MovAction = movementAction;
            this.NextAction = nextAction;
        }

        public bool performAction()
        {
            switch(MovAction)
            {
                case MovementAction.EnterAnimalBuilding:
                    return TryToWarp(Destination.X, Destination.Y - 1);
                    break;

                case MovementAction.EnterFarmHouse:
                    Console.WriteLine("Enter Farm House");
                    return false;
                    break;

                case MovementAction.EnterGreenhouse:
                    Console.WriteLine("Enter Greenhouse");
                    return false;
                    break;

                default:
                    return false;

            }
        }


        public bool TryToWarp(int warpX, int warpY)
        {
            Warp w2 = Game1.currentLocation.isCollidingWithWarpOrDoor(new Microsoft.Xna.Framework.Rectangle(18 * 64, 11 * 64, 64, 64), Game1.player);
            if (w2 != null)
            {
                Game1.player.warpFarmer(w2);
                return true;
            }
            else
                return false;
        }
    }
}
