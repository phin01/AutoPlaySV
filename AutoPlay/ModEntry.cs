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
using StardewValley.TerrainFeatures;
using System.Threading;
using StardewValley.Menus;
using StardewValley.Objects;

namespace AutoPlaySV
{
    public class ModEntry : Mod
    {
        public enum Action
        {
            leaveHouse,
            
            findAnimalBuildings,
            moveToAnimalBuildings,
            updateAnimalBuildingList,
            
            getAnimalsList,
            moveToAnimal,
            petAnimal,
            updateAnimalList,
            exitAnimalHouse,

            getProductMakerList,
            moveToProductMaker,
            updateProductMakerList,

            awaitDestination,
            halt,
            noAction
        }
        
        
        private static PathFindController pathFind;
        private List<Building> animalBuildings;
        private List<FarmAnimal> animalsToPet;
        private List<ClosestAnimal> closestAnimalsToPet;
        private List<StardewValley.Object> productMakers;
        private Point destinationChecker;
        private MovementDestination movementDestination;
        private Action nextAction;
        private Action fallbackAction;


        // Disable event checker and auto movement for testing
        private bool debugMode = false;


        // Events
        private EventHandler DayHasStarted;


        // Event variable checks
        private static bool autoPlayActive;
        

        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            
            if(!debugMode)
                autoPlayActive = true;

            this.DayHasStarted += ExitFarmHouse;
            nextAction = Action.leaveHouse;

        }


        private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if(!debugMode)
                CheckEvent();

            if (autoPlayActive)
            {
                try
                {
                    pathFind.update(Game1.currentGameTime);
                    Game1.player.updateMovement(Game1.player.currentLocation, Game1.currentGameTime);
                    Game1.player.updateMovementAnimation(Game1.currentGameTime);
                }
                catch (Exception)
                {

                }
            }
            
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button == SButton.P)
            {
                // leave house
                if (Game1.currentLocation.Name == "FarmHouse")
                {
                    Warp w2 = Game1.currentLocation.isCollidingWithWarpOrDoor(new Microsoft.Xna.Framework.Rectangle(12 * 64, 21 * 64, 64, 64), Game1.player);
                    this.Monitor.Log($"W2: {w2}", LogLevel.Debug);
                    Game1.player.warpFarmer(w2);
                }

                if (Game1.currentLocation.Name == "Farm")
                {
                    Game1.player.position.X = 704;
                    Game1.player.position.Y = 1312;
                }
            }


            if (e.Button == SButton.L)
            {
                //productMakers = new List<StardewValley.Object>();

                //foreach (StardewValley.Object obj in Game1.currentLocation.objects.Values)
                //{
                //    if (obj.Name.Contains("Cheese Press") || obj.Name.Contains("Mayonnaise Machine"))
                //    {
                //        productMakers.Add(obj);
                //        //this.Monitor.Log($"Object: {obj.Name} at {obj.tileLocation.X}:{obj.tileLocation.Y}", LogLevel.Debug);
                //        //this.Monitor.Log($"Action: {obj.checkForAction(Game1.player)}", LogLevel.Debug);
                //    }
                //}

                //foreach (StardewValley.Object obj in productMakers)
                //{
                //    obj.checkForAction(Game1.player);
                //}

                nextAction = Action.getProductMakerList;
                this.Monitor.Log($"Debug Mode Disabled", LogLevel.Debug);
                debugMode = false;
            }


            if (e.Button == SButton.O)
            {
                this.Monitor.Log($"Player Position - X: {Game1.player.getTileX()} at Y: {Game1.player.getTileY()}", LogLevel.Debug);
                this.Monitor.Log($"Player Position - X: {Game1.player.position.X} at Y: {Game1.player.position.Y}", LogLevel.Debug);
            }

            if (e.Button == SButton.K)
            {
                autoPlayActive = true;
                this.Monitor.Log($"AutoPlay: {autoPlayActive}", LogLevel.Debug);
            }

            if (e.Button == SButton.I)
            {
                autoPlayActive = false;
                this.Monitor.Log($"AutoPlay: {autoPlayActive}", LogLevel.Debug);
            }
        }


        // ***************************************************** 
        // EVENT FUNCTIONS
        // *****************************************************
        private void ExitFarmHouse(object sender, EventArgs e)
        {
            if (Game1.currentLocation.Name == "FarmHouse")
            {
                foreach (Warp warp in Game1.currentLocation.warps)
                {
                    if (warp.TargetName == "Farm")
                    {
                        Point mainHouseDoor = new Point(warp.X, warp.Y);
                        this.Monitor.Log($"Moving to Destination: {mainHouseDoor}", LogLevel.Debug);
                        MoveToLocationBool(mainHouseDoor);
                    }
                }
            }
        }



        // *****************************************************
        // EVENT RAISER
        // *****************************************************
        private void CheckEvent()
        {
            // Leave the house after waking up (triggers before 7am)
            if (Game1.currentLocation.Name == "FarmHouse" && nextAction == Action.leaveHouse)
            {
                this.DayHasStarted?.Invoke(this, EventArgs.Empty);
                this.Monitor.Log($"Leaving Farm House", LogLevel.Debug);
                nextAction = Action.halt;
            }

            //Stop movement after leaving house
            if (Game1.currentLocation.Name == "Farm" && nextAction == Action.halt)
            {
                autoPlayActive = false;
                this.Monitor.Log($"Halt movement", LogLevel.Debug);
                nextAction = Action.findAnimalBuildings;
            }

            // Find Animal Buildings
            if (Game1.currentLocation.Name == "Farm" && nextAction == Action.findAnimalBuildings)
            {
                autoPlayActive = true;
                animalBuildings = GetAnimalBuildings();
                if(animalBuildings.Count > 0)
                {
                    nextAction = Action.moveToAnimalBuildings;
                    this.Monitor.Log($"Found {animalBuildings.Count} Animal Buildings", LogLevel.Debug);
                }
                autoPlayActive = true;
            }

            // Move To Animal Building
            if (Game1.currentLocation.Name == "Farm" && nextAction == Action.moveToAnimalBuildings)
            {
                if (animalBuildings.Count > 0)
                {
                    destinationChecker = new Point(animalBuildings[0].getPointForHumanDoor().X, animalBuildings[0].getPointForHumanDoor().Y + 1);
                    movementDestination = new MovementDestination(destinationChecker, MovementDestination.MovementAction.EnterAnimalBuilding, Action.getAnimalsList);
                    
                    MoveToLocationBool(destinationChecker);
                    autoPlayActive = true;

                    this.Monitor.Log($"Moving to  {animalBuildings[0].buildingType} at X: {destinationChecker.X} Y: {destinationChecker.Y}", LogLevel.Debug);
                    nextAction = Action.awaitDestination;
                    fallbackAction = Action.noAction;
                }
            }

            // ****************************************************
            // Destination Checker
            // ****************************************************
            if (nextAction == Action.awaitDestination)
            {
                try
                {
                    if (pathFind.pathToEndPoint.Count > 0)
                    {
                        if (Game1.player.getTileX() == destinationChecker.X && Game1.player.getTileY() == destinationChecker.Y)
                        {
                            this.Monitor.Log($"Reached Destination X: {destinationChecker.X} Y: {destinationChecker.Y}", LogLevel.Debug);
                            bool actionResult = movementDestination.performAction();
                            this.Monitor.Log($"Action result: {actionResult}", LogLevel.Debug);
                            if (actionResult)
                            {
                                autoPlayActive = false;
                                nextAction = movementDestination.NextAction;
                            }
                            else
                                this.Monitor.Log($"Could not perform Destination Action at X: {destinationChecker.X} Y: {destinationChecker.Y}", LogLevel.Debug);
                        }
                    }
                }
                catch (Exception)
                {
                    this.Monitor.Log($"Caught error in awaitDestination", LogLevel.Debug);
                    if (fallbackAction == Action.noAction)
                    {
                        autoPlayActive = false;
                        MoveToLocationBool(destinationChecker);
                        autoPlayActive = true;
                    }
                    else
                    {
                        nextAction = fallbackAction;
                        fallbackAction = Action.noAction;
                    }
                }

                /*
                if (testPathFind.pathToEndPoint.Count > 0)
                {
                    if (Game1.player.getTileX() == destinationChecker.X && Game1.player.getTileY() == destinationChecker.Y)
                    {
                        this.Monitor.Log($"Reached Destination X: {destinationChecker.X} Y: {destinationChecker.Y}", LogLevel.Debug);
                        bool actionResult = movementDestination.performAction();
                        if (actionResult)
                        {
                            autoPlayActive = false;
                            nextAction = movementDestination.NextAction;
                        }
                        else
                            this.Monitor.Log($"Could not perform Destination Action at X: {destinationChecker.X} Y: {destinationChecker.Y}", LogLevel.Debug);
                    }
                }
                else
                {
                    if(fallbackAction == Action.noAction)
                    {
                        autoPlayActive = false;
                        MoveToLocation(destinationChecker);
                        startTimeOfMovement = Game1.timeOfDay;
                        autoPlayActive = true;
                    }
                    else
                    {
                        nextAction = fallbackAction;
                        fallbackAction = Action.noAction;
                    }
                    
                }
                */

            }

            // Get Animals List
            if ((Game1.currentLocation.Name.Contains("Barn") || Game1.currentLocation.Name.Contains("Coop")) && nextAction == Action.getAnimalsList) 
            {
                this.Monitor.Log($"Getting List of Animals to Pet", LogLevel.Debug);
                animalsToPet = GetAnimalsList();

                closestAnimalsToPet = new List<ClosestAnimal>();

                foreach (FarmAnimal farmAnimal in animalsToPet)
                {
                    closestAnimalsToPet.Add(new ClosestAnimal(farmAnimal));
                }

                if (closestAnimalsToPet.Count > 0)
                    nextAction = Action.moveToAnimal;
                else
                    nextAction = Action.noAction;

            }

            // Move to Animal
            if ((Game1.currentLocation.Name.Contains("Barn") || Game1.currentLocation.Name.Contains("Coop")) && nextAction == Action.moveToAnimal)
            {
                if (closestAnimalsToPet.Count > 0)
                {
                    bool successfulPath = false;
                    autoPlayActive = true;
                    int loopingCounter = 0;

                    closestAnimalsToPet.Sort((x, y) => x.SortedPosition().CompareTo(y.SortedPosition()));

                    while (!successfulPath)
                    {
                        destinationChecker = new Point((int)closestAnimalsToPet[0].GetAnimal().position.X / 64, (int)closestAnimalsToPet[0].GetAnimal().position.Y / 64); ;
                        destinationChecker = FindNearbyUnoccupiedPoint(destinationChecker);
                        successfulPath = MoveToLocationBool(destinationChecker);
                        loopingCounter++;
                        this.Monitor.Log($"Looping... {loopingCounter} times", LogLevel.Debug);
                    }

                    movementDestination = new MovementDestination(destinationChecker, MovementDestination.MovementAction.MoveToAnimal, Action.updateAnimalList, closestAnimalsToPet[0].GetAnimal());
                    this.Monitor.Log($"Moving to Animal {closestAnimalsToPet[0].GetAnimal().name} at X: {destinationChecker.X} Y: {destinationChecker.Y}", LogLevel.Debug);
                    nextAction = Action.awaitDestination;
                    fallbackAction = Action.moveToAnimal;
                }
            }

            // After petting, update Animal List and restart movement/petting cycle
            if (nextAction == Action.updateAnimalList)
            {
                if (closestAnimalsToPet.Count > 1)
                {
                    closestAnimalsToPet.RemoveAt(0);
                    nextAction = Action.moveToAnimal;
                }
                else
                {
                    closestAnimalsToPet.RemoveAt(0);
                    nextAction = Action.exitAnimalHouse;
                }

            }

            // Leave Animal House after all tasks have been completed
            if (nextAction == Action.exitAnimalHouse)
            {
                autoPlayActive = true;
                this.Monitor.Log($"Leaving {animalBuildings[0].buildingType}", LogLevel.Debug);
                ExitBuildingToFarm();
                nextAction = Action.updateAnimalBuildingList;
            }

            // After completing tasks in Animal House, update List and restart cycle for next Houses
            if (Game1.currentLocation.Name == "Farm" && nextAction == Action.updateAnimalBuildingList)
            {
                autoPlayActive = false;
                if (animalBuildings.Count > 1)
                {
                    animalBuildings.RemoveAt(0);
                    nextAction = Action.moveToAnimalBuildings;
                }
                else
                {
                    animalBuildings.RemoveAt(0);
                    //nextAction = Action.noAction;
                    nextAction = Action.getProductMakerList;
                }
            }

            // Get List of Product Makers (Mayonnaise Machine and Cheese Press)
            if (Game1.currentLocation.Name == "Farm" && nextAction == Action.getProductMakerList)
            {
                productMakers = new List<StardewValley.Object>();

                foreach (StardewValley.Object obj in Game1.currentLocation.objects.Values)
                {
                    if (obj.Name.Contains("Cheese Press") || obj.Name.Contains("Mayonnaise Machine"))
                    {
                        productMakers.Add(obj);
                    }
                }
                nextAction = Action.moveToProductMaker;
            }

            // Move to Product Maker (Mayonnaise Machine and Cheese Press)
            if (Game1.currentLocation.Name == "Farm" && nextAction == Action.moveToProductMaker)
            {
                if (productMakers.Count > 0)
                {
                    bool successfulPath = false;
                    autoPlayActive = true;
                    int loopingCounter = 0;

                    while (!successfulPath)
                    {
                        destinationChecker = new Point((int)productMakers[0].tileLocation.X, (int)productMakers[0].tileLocation.Y);
                        destinationChecker = FindNearbyUnoccupiedPoint(destinationChecker);
                        successfulPath = MoveToLocationBool(destinationChecker);
                        loopingCounter++;
                        this.Monitor.Log($"Looping... {loopingCounter} times", LogLevel.Debug);
                    }

                    movementDestination = new MovementDestination(destinationChecker, MovementDestination.MovementAction.MoveToProductMaker, Action.updateProductMakerList, productMakers[0]);
                    //this.Monitor.Log($"Result: {movementDestination.performAction()}", LogLevel.Debug);
                    productMakers[0].checkForAction(Game1.player);
                    this.Monitor.Log($"Moving to {productMakers[0].name} at X: {destinationChecker.X} Y: {destinationChecker.Y}", LogLevel.Debug);
                    nextAction = Action.awaitDestination;
                    fallbackAction = Action.moveToProductMaker;
                }
            }

            // After getting Product from Product Maker, update Product Maker List and restart cycle
            if (nextAction == Action.updateProductMakerList)
            {
                if (productMakers.Count > 1)
                {
                    productMakers.RemoveAt(0);
                    nextAction = Action.moveToProductMaker;
                }
                else
                {
                    productMakers.RemoveAt(0);
                    nextAction = Action.noAction;
                }
            }




        }






        // *****************************************************
        // AUX FUNCTIONS
        // *****************************************************
        private void MoveToLocation(Point destination, bool searchNearby = true)
        {
            //this.Monitor.Log($"Moving to Destination: {destination}", LogLevel.Debug);
            if (searchNearby && Game1.player.currentLocation.isTileOccupiedForPlacement(new Vector2(destination.X, destination.Y)))
            //if (searchNearby && Game1.player.currentLocation.isTileOccupied(new Vector2(destination.X, destination.Y)))
            {
                this.Monitor.Log($"Tile {destination} occupied", LogLevel.Debug);
                destination = FindNearbyUnoccupiedPoint(destination);
            }

            pathFind = new PathFindController(Game1.player, Game1.player.currentLocation, destination, 0);
        }

        private bool MoveToLocationBool(Point destination)
        {
            PathFindController testPathFind = new PathFindController(Game1.player, Game1.player.currentLocation, destination, 0);
            try
            {
                if (testPathFind.pathToEndPoint.Count > 0)
                {
                    pathFind = testPathFind;
                    return true;
                }
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
            
        }

        private List<Building> GetAnimalBuildings()
        {
            List<Building> listBuildings = new List<Building>();

            foreach (Building b in Game1.getFarm().buildings)
            {
                //this.Monitor.Log($"Building Type: {b.buildingType}", LogLevel.Debug);
                if (b.buildingType.Contains("Barn") || b.buildingType.Contains("Coop"))
                {
                    listBuildings.Add(b);
                    this.Monitor.Log($"Tile {b.tileX} occupied", LogLevel.Debug);
                }
                    
            }
            return listBuildings;
        }

        private List<FarmAnimal> GetAnimalsList()
        {
            List<FarmAnimal> animalList = new List<FarmAnimal>();

            if (Game1.currentLocation.GetType() == typeof(AnimalHouse))
            {
                AnimalHouse farm = (AnimalHouse)Game1.currentLocation;

                int contadorAnimais = ((NetDictionary<long, FarmAnimal, NetRef<FarmAnimal>, SerializableDictionary<long, FarmAnimal>, NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>>>)(object)farm.animals).Count();
                this.Monitor.Log($"Animais 1: {contadorAnimais}", LogLevel.Debug);

                NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>> listaAnimais = farm.Animals;
                this.Monitor.Log($"Animais 2: {listaAnimais.Count()}", LogLevel.Debug);

                foreach (KeyValuePair<long, FarmAnimal> animalzin in listaAnimais.Pairs)
                {
                    animalList.Add(animalzin.Value);
                    //animalzin.Value.pet(Game1.player);
                }
            }
            return animalList;
        }

        private Point FindNearbyUnoccupiedPoint(Point originalDestination)
        {
            Point[] offsets = new Point[17]
            {
                new Point(0, 0),
                new Point(-1, 0),
                new Point(1, 0),
                new Point(0, -1),
                new Point(0, 1),
                new Point(-1, -1),
                new Point(1, 1),
                new Point(-1, 1),
                new Point(1, -1),
                new Point(-2, 0),
                new Point(2, 0),
                new Point(0, -2),
                new Point(0, 2),
                new Point(-2, -2),
                new Point(2, 2),
                new Point(-2, 2),
                new Point(2, -2)
            };

            foreach(Point offset in offsets)
            {
                Point newPointToCheck = new Point(originalDestination.X + offset.X, originalDestination.Y + offset.Y);
                if (!Game1.player.currentLocation.isTileOccupiedForPlacement(new Vector2(newPointToCheck.X, newPointToCheck.Y)))
                    return newPointToCheck;
                else
                    this.Monitor.Log($"Tile {newPointToCheck} occupied", LogLevel.Debug);
            }
            return originalDestination;
        }

        private void ExitBuildingToFarm()
        {
            bool successfulPath = false;
            int loopingCounter = 0;

            foreach (Warp warp in Game1.currentLocation.warps)
            {
                if (warp.TargetName == "Farm")
                {
                    Point exitDoor = new Point(warp.X, warp.Y);
                    this.Monitor.Log($"Moving to Destination: {exitDoor}", LogLevel.Debug);

                    while (!successfulPath)
                    {
                        successfulPath = MoveToLocationBool(exitDoor);
                        loopingCounter++;
                        this.Monitor.Log($"Looping... {loopingCounter} times", LogLevel.Debug);
                    }
                }
            }
        }

        private void auxAutoGrabberRetrieval()
        {
            /*
            if (e.Button == SButton.P)
            {
                // leave house
                if (Game1.currentLocation.Name == "FarmHouse")
                {
                    Warp w2 = Game1.currentLocation.isCollidingWithWarpOrDoor(new Microsoft.Xna.Framework.Rectangle(12 * 64, 21 * 64, 64, 64), Game1.player);
                    this.Monitor.Log($"W2: {w2}", LogLevel.Debug);
                    Game1.player.warpFarmer(w2);
                }

                // go to barn
                if (Game1.currentLocation.Name == "Farm")
                {
                    Warp w2 = Game1.currentLocation.isCollidingWithWarpOrDoor(new Microsoft.Xna.Framework.Rectangle(18 * 64, 11 * 64, 64, 64), Game1.player);
                    Game1.player.warpFarmer(w2);
                }

                if (Game1.currentLocation.Name.Contains("Barn"))
                {
                    AnimalHouse farm = (AnimalHouse)Game1.currentLocation;

                    //GameTime startOfFunction = new Game1.currentGameTime;
                    this.Monitor.Log($"GameTime Start : {Game1.currentGameTime.TotalGameTime.Ticks}", LogLevel.Debug);

                    foreach (StardewValley.Object obj in Game1.currentLocation.objects.Values)
                    {
                        if (obj.Name.Contains("Auto-Grabber"))
                        {
                            this.Monitor.Log($"Object Name: {obj.Name}", LogLevel.Debug);
                            //this.Monitor.Log($"Object Position   X: {obj.tileLocation.X}  Y: {obj.tileLocation.Y}", LogLevel.Debug);
                            //this.Monitor.Log($"Object Parent Sheet Index : {obj.parentSheetIndex}", LogLevel.Debug);
                            //this.Monitor.Log($"Object Use Action : {obj.performUseAction(Game1.currentLocation)}", LogLevel.Debug);
                            //this.Monitor.Log($"Object Use Action : {obj.performUseAction(farm)}", LogLevel.Debug);

                            Game1.activeClickableMenu = new ItemGrabMenu((obj.heldObject.Value as Chest).items, reverseGrab: false, showReceivingMenu: true, InventoryMenu.highlightAllItems, (obj.heldObject.Value as Chest).grabItemFromInventory, null, null, snapToBottom: false, canBeExitedWithKey: true, playRightClickSound: true, allowRightClick: true, showOrganizeButton: true, 1, obj, -1, this);

                            this.Monitor.Log($"Grabber Item count : {(obj.heldObject.Value as Chest).items.Count}", LogLevel.Debug);

                            foreach (Item item in (obj.heldObject.Value as Chest).items)
                            {
                                this.Monitor.Log($"Item Name : {item.Name}", LogLevel.Debug);
                                Game1.player.addItemToInventory(item);
                            }

                            while ((obj.heldObject.Value as Chest).items.Count > 0)
                            {
                                foreach (Item item in (obj.heldObject.Value as Chest).items)
                                {
                                    (obj.heldObject.Value as Chest).items.Remove(item);
                                }
                            }

                            //Game1.activeClickableMenu.exitThisMenu();
                            this.Monitor.Log($"GameTime End : {Game1.currentGameTime.TotalGameTime.Ticks}", LogLevel.Debug);
                        }
                    }

                }
                
                // X:12, Y: 13 - Coletor
                // ShippingBin class -> shipItem method
            }
            */
        }


    }
}
