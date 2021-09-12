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
            EnterGreenhouse,
            MoveToAnimal,
            MoveToProductMaker,
            MoveToAutoGrabber
        }

        public Point Destination { get; set; }
        
        public MovementAction MovAction { get; set; }

        public ModEntry.Action NextAction { get; set; }

        //public object ParamObject { get; set; }
        public StardewValley.Object ParamObject { get; set; }

        public FarmAnimal ParamFarmAnimal { get; set; }


        public MovementDestination(Point destination, MovementAction movementAction, ModEntry.Action nextAction)
        {
            this.Destination = destination;
            this.MovAction = movementAction;
            this.NextAction = nextAction;
            //this.ParamFarmAnimal = paramFarmAnimal;
            //this.ParamObject = paramObject;
        }

        public MovementDestination(Point destination, MovementAction movementAction, ModEntry.Action nextAction, FarmAnimal paramFarmAnimal)
        {
            this.Destination = destination;
            this.MovAction = movementAction;
            this.NextAction = nextAction;
            this.ParamFarmAnimal = paramFarmAnimal;
            //this.ParamObject = paramObject;
        }

        public MovementDestination(Point destination, MovementAction movementAction, ModEntry.Action nextAction, StardewValley.Object paramObject)
        {
            this.Destination = destination;
            this.MovAction = movementAction;
            this.NextAction = nextAction;
            //this.ParamFarmAnimal = paramFarmAnimal;
            this.ParamObject = paramObject;
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

                case MovementAction.MoveToAnimal:
                    return TryToPetAnimal(ParamFarmAnimal);
                    break;

                case MovementAction.MoveToProductMaker:
                    return GetProductFromMaker(ParamObject);
                    break;

                case MovementAction.MoveToAutoGrabber:
                    return TryToOpenAutoGrabber(ParamObject);
                    break;

                default:
                    return false;

            }
        }


        public bool TryToWarp(int warpX, int warpY)
        {
            //Warp w2 = Game1.currentLocation.isCollidingWithWarpOrDoor(new Microsoft.Xna.Framework.Rectangle(18 * 64, 11 * 64, 64, 64), Game1.player);
            Warp w2 = Game1.currentLocation.isCollidingWithWarpOrDoor(new Microsoft.Xna.Framework.Rectangle(warpX * 64, warpY * 64, 64, 64), Game1.player);
            if (w2 != null)
            {
                Game1.player.warpFarmer(w2);
                return true;
            }
            else
                return false;
        }

        public bool TryToPetAnimal(FarmAnimal animal)
        {
            try
            {
                animal.pet(Game1.player);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool GetProductFromMaker(StardewValley.Object productMaker)
        {
            try
            {
                productMaker.checkForAction(Game1.player);
                Item product = null;

                if(productMaker.name.Contains("Cheese"))
                    product = Game1.player.hasItemWithNameThatContains("Milk");
                else if(productMaker.name.Contains("Mayonnaise"))
                    product = Game1.player.hasItemWithNameThatContains("Egg");
                
                if (product != null)
                {
                    productMaker.performObjectDropInAction(product, false, Game1.player);
                    product.Stack--;
                    if(product.Stack == 0)
                        Game1.player.removeItemFromInventory(product);
                }
                    

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    
        public bool TryToOpenAutoGrabber(StardewValley.Object autoGrabber)
        {
            try
            {
                autoGrabber.checkForAction(Game1.player);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    
    
    }
}
