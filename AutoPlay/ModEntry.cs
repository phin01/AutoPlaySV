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
            awaitDestination,
            halt,
            noAction
        }
        
        
        private static PathFindController pathFind;
        private List<Building> animalBuildings;
        private List<FarmAnimal> animalsToPet;
        private Point destinationChecker;
        private MovementDestination movementDestination;
        private Action nextAction;
        private Action fallbackAction;


        // Events
        private EventHandler DayHasStarted;


        // Event variable checks
        private static bool autoPlayActive;
        

        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            autoPlayActive = true;

            this.DayHasStarted += ExitFarmHouse;
            nextAction = Action.leaveHouse;

        }


        private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

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
                autoPlayActive = true;
                ExitBuildingToFarm();
                autoPlayActive = false;
            }

            if (e.Button == SButton.O)
            {
                this.Monitor.Log($"Player Position - X: {Game1.player.getTileX()} at Y: {Game1.player.getTileY()}", LogLevel.Debug);
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
                if(animalsToPet.Count > 0)
                    nextAction = Action.moveToAnimal;
                else
                    nextAction = Action.noAction;
            }

            // Move to Animal
            if ((Game1.currentLocation.Name.Contains("Barn") || Game1.currentLocation.Name.Contains("Coop")) && nextAction == Action.moveToAnimal)
            {
                if(animalsToPet.Count > 0)
                {
                    bool successfulPath = false;
                    autoPlayActive = true;
                    int loopingCounter = 0;

                    while (!successfulPath)
                    {
                        destinationChecker = new Point((int)animalsToPet[0].position.X / 64, (int)animalsToPet[0].position.Y / 64);
                        destinationChecker = FindNearbyUnoccupiedPoint(destinationChecker);
                        successfulPath = MoveToLocationBool(destinationChecker);
                        loopingCounter++;
                        this.Monitor.Log($"Looping... {loopingCounter} times", LogLevel.Debug);
                    }

                    movementDestination = new MovementDestination(destinationChecker, MovementDestination.MovementAction.MoveToAnimal, Action.updateAnimalList, animalsToPet[0]);
                    this.Monitor.Log($"Moving to Animal {animalsToPet[0].name} at X: {destinationChecker.X} Y: {destinationChecker.Y}", LogLevel.Debug);
                    nextAction = Action.awaitDestination;
                    fallbackAction = Action.moveToAnimal;

                    /*autoPlayActive = true;
                    destinationChecker = new Point((int)animalsToPet[0].position.X / 64, (int)animalsToPet[0].position.Y / 64);
                    destinationChecker = FindNearbyUnoccupiedPoint(destinationChecker);
                    movementDestination = new MovementDestination(destinationChecker, MovementDestination.MovementAction.MoveToAnimal, Action.updateAnimalList, animalsToPet[0]);
                    MoveToLocation(destinationChecker, searchNearby: false);

                    this.Monitor.Log($"Moving to Animal {animalsToPet[0].name} at X: {destinationChecker.X} Y: {destinationChecker.Y}", LogLevel.Debug);
                    nextAction = Action.awaitDestination;
                    fallbackAction = Action.moveToAnimal;
                    */
                }
            }

            // After petting, update Animal List and restart movement/petting cycle
            if (nextAction == Action.updateAnimalList)
            {
                if(animalsToPet.Count > 1)
                {
                    animalsToPet.RemoveAt(0);
                    nextAction = Action.moveToAnimal;
                }
                else
                {
                    animalsToPet.RemoveAt(0);
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

    }
}
