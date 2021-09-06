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
            getAnimalsList,
            moveToAnimal,
            petAnimal,
            updateAnimalList,
            exitAnimalHouse,
            awaitDestination,
            halt,
            noAction
        }
        
        
        private static PathFindController testPathFind;
        private List<Building> animalBuildings;
        private List<FarmAnimal> animalsToPet;
        private Point destinationChecker;
        private MovementDestination movementDestination;
        private Action nextAction;
        private Action fallbackAction;
        private int startTimeOfMovement;


        // Events
        private EventHandler DayHasStarted;


        // Event variable checks
        private static bool autoPlayActive;
        

        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            autoPlayActive = true;

            this.DayHasStarted += ExitMainHouse;
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
                    testPathFind.update(Game1.currentGameTime);
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
                Point nearby = FindNearbyUnoccupiedTileThatFitsCharacter(Game1.player.currentLocation, 38, 13);
                this.Monitor.Log($"Nearby Point Available -  X: {nearby.X}  Y: {nearby.Y}", LogLevel.Debug);
                //this.GetAnimalBuildings();
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
        private void ExitMainHouse(object sender, EventArgs e)
        {
            if (Game1.currentLocation.Name == "FarmHouse")
            {
                foreach (Warp warp in Game1.currentLocation.warps)
                {
                    if (warp.TargetName == "Farm")
                    {
                        Point mainHouseDoor = new Point(warp.X, warp.Y);
                        this.Monitor.Log($"Moving to Destination: {mainHouseDoor}", LogLevel.Debug);
                        MoveToLocation(mainHouseDoor);
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
                autoPlayActive = true;
                if (animalBuildings.Count > 0)
                {
                    destinationChecker = new Point(animalBuildings[0].getPointForHumanDoor().X, animalBuildings[0].getPointForHumanDoor().Y + 1);
                    movementDestination = new MovementDestination(destinationChecker, MovementDestination.MovementAction.EnterAnimalBuilding, Action.getAnimalsList);
                    
                    MoveToLocation(destinationChecker);
                    this.Monitor.Log($"Moving to  {animalBuildings[0].buildingType} at X: {destinationChecker.X} Y: {destinationChecker.Y}", LogLevel.Debug);
                    nextAction = Action.awaitDestination;
                    fallbackAction = Action.noAction;
                    startTimeOfMovement = Game1.timeOfDay;
                }
            }

            // Destination Checker
            if (nextAction == Action.awaitDestination)
            {
                //this.Monitor.Log($"Time of day elapsed: {Game1.timeOfDay - startTimeOfMovement}", LogLevel.Debug);
                
                if(Game1.timeOfDay - startTimeOfMovement < 10)
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
                    autoPlayActive = true;
                    destinationChecker = new Point((int)animalsToPet[0].position.X / 64, (int)animalsToPet[0].position.Y / 64);
                    movementDestination = new MovementDestination(destinationChecker, MovementDestination.MovementAction.MoveToAnimal, Action.updateAnimalList, animalsToPet[0]);

                    MoveToLocation(destinationChecker);
                    this.Monitor.Log($"Moving to Animal {animalsToPet[0].name} at X: {destinationChecker.X} Y: {destinationChecker.Y}", LogLevel.Debug);
                    nextAction = Action.awaitDestination;
                    fallbackAction = Action.moveToAnimal;
                    startTimeOfMovement = Game1.timeOfDay;
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
                this.Monitor.Log($"Leaving {animalBuildings[0].buildingType}", LogLevel.Debug);
                nextAction = Action.noAction;
            }






        }






        // *****************************************************
        // AUX FUNCTIONS
        // *****************************************************
        private void MoveToLocation(Point destination)
        {
            //this.Monitor.Log($"Moving to Destination: {destination}", LogLevel.Debug);
            if (Game1.player.currentLocation.isTileOccupiedForPlacement(new Vector2(destination.X, destination.Y)))
            {
                this.Monitor.Log($"Tile {destination} occupied", LogLevel.Debug);
                destination = FindNearbyUnoccupiedPoint(destination);
            }
                

            testPathFind = new PathFindController(Game1.player, Game1.player.currentLocation, destination, 0);
            //this.Monitor.Log($"testPathFind: {testPathFind}", LogLevel.Debug);
        }

        private List<Building> GetAnimalBuildings()
        {
            List<Building> listBuildings = new List<Building>();

            foreach (Building b in Game1.getFarm().buildings)
            {
                //this.Monitor.Log($"Building Type: {b.buildingType}", LogLevel.Debug);
                if (b.buildingType.Contains("Barn") || b.buildingType.Contains("Coop"))
                    listBuildings.Add(b);
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
            Point[] offsets = new Point[16]
            {
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



        public Point FindNearbyUnoccupiedTileThatFitsCharacter(GameLocation location, int target_x, int target_y, int width = 1, Point? invalid_tile = null)
        {
            HashSet<Point> visited_tiles = new HashSet<Point>();
            List<Point> open_tiles = new List<Point>();
            open_tiles.Add(new Point(target_x, target_y));
            visited_tiles.Add(new Point(target_x, target_y));
            Point[] offsets = new Point[4]
            {
                new Point(-1, 0),
                new Point(1, 0),
                new Point(0, -1),
                new Point(0, 1)
            };
            for (int i = 0; i < 500; i++)
            {
                if (open_tiles.Count == 0)
                {
                    break;
                }
                Point tile = open_tiles[0];
                open_tiles.RemoveAt(0);
                Point[] array = offsets;
                for (int j = 0; j < array.Length; j++)
                {
                    Point offset = array[j];
                    Point next_tile = new Point(tile.X + offset.X, tile.Y + offset.Y);
                    if (!visited_tiles.Contains(next_tile))
                    {
                        open_tiles.Add(next_tile);
                    }
                }
                if (visited_tiles.Contains(tile) || (invalid_tile.HasValue && tile.X == invalid_tile.Value.X && tile.Y == invalid_tile.Value.Y))
                {
                    continue;
                }
                visited_tiles.Add(tile);
                bool fail = false;
                int height = 1;
                for (int w = 0; w < width; w++)
                {
                    for (int h = 0; h < height; h++)
                    {
                        Point checked_tile = new Point(tile.X + w, tile.Y + h);
                        new Microsoft.Xna.Framework.Rectangle(checked_tile.X * 64, checked_tile.Y * 64, 64, 64).Inflate(-4, -4);
                        if (checked_tile.X == target_x && checked_tile.Y == target_y + 1)
                        {
                            fail = true;
                            break;
                        }
                        if (invalid_tile.HasValue && invalid_tile.Value == checked_tile)
                        {
                            fail = true;
                            break;
                        }
                        if (!location.isTileLocationOpenIgnoreFrontLayers(new xTile.Dimensions.Location(checked_tile.X, checked_tile.Y)))
                        {
                            fail = true;
                            break;
                        }
                        if (location.isObjectAtTile(checked_tile.X, checked_tile.Y))
                        {
                            fail = true;
                            break;
                        }
                        if (location.isTerrainFeatureAt(checked_tile.X, checked_tile.Y))
                        {
                            fail = true;
                            break;
                        }
                        if (fail)
                        {
                            continue;
                        }
                        Microsoft.Xna.Framework.Rectangle tile_rect = new Microsoft.Xna.Framework.Rectangle(checked_tile.X * 64, checked_tile.Y * 64, 64, 64);
                        foreach (ResourceClump resourceClump in location.resourceClumps)
                        {
                            if (resourceClump.getBoundingBox(resourceClump.tile).Intersects(tile_rect))
                            {
                                fail = true;
                                break;
                            }
                        }
                    }
                }
                if (!fail)
                {
                    return tile;
                }
            }
            return new Point(target_x, target_y);
        }



    }
}
