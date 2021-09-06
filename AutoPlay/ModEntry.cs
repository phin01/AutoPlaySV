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
    public class ModEntry : Mod
    {
        public enum NextAction
        {
            leaveHouse,
            findAnimalBuildings,
            moveToAnimalBuildings,
            petAnimals,
            awaitDestination,
            halt,
            noAction
        }
        
        
        private static PathFindController testPathFind;
        private List<Building> animalBuildings;
        private Point destinationChecker;
        private MovementDestination movementDestination;
        private NextAction nextAction;


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
            nextAction = NextAction.leaveHouse;

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
                this.FindAnimalBuildings();
            }

            if (e.Button == SButton.O)
            {
            }

            if (e.Button == SButton.L)
            {
                autoPlayActive = true;
            }

            if (e.Button == SButton.I)
            {
                autoPlayActive = false;
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

        private void PetAnimals()
        {
            if (Game1.currentLocation.GetType() == typeof(AnimalHouse))
            {
                AnimalHouse farm = (AnimalHouse)Game1.currentLocation;

                int contadorAnimais = ((NetDictionary<long, FarmAnimal, NetRef<FarmAnimal>, SerializableDictionary<long, FarmAnimal>, NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>>>)(object)farm.animals).Count();
                this.Monitor.Log($"Animais 1: {contadorAnimais}", LogLevel.Debug);

                NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>> listaAnimais = farm.Animals;
                this.Monitor.Log($"Animais 2: {listaAnimais.Count()}", LogLevel.Debug);

                foreach (KeyValuePair<long, FarmAnimal> animalzin in listaAnimais.Pairs)
                {
                    animalzin.Value.pet(Game1.player);
                }
            }
        }



        // *****************************************************
        // EVENT RAISER
        // *****************************************************
        private void CheckEvent()
        {
            // Leave the house after waking up (triggers before 7am)
            if (Game1.currentLocation.Name == "FarmHouse" && Game1.timeOfDay < 700 && nextAction == NextAction.leaveHouse)
            {
                this.DayHasStarted?.Invoke(this, EventArgs.Empty);
                this.Monitor.Log($"Leaving Farm House", LogLevel.Debug);
                nextAction = NextAction.halt;
            }

            //Stop movement after leaving house
            if (Game1.currentLocation.Name == "Farm" && nextAction == NextAction.halt)
            {
                autoPlayActive = false;
                this.Monitor.Log($"Halt movement", LogLevel.Debug);
                nextAction = NextAction.findAnimalBuildings;
            }

            // Find Animal Buildings
            if (Game1.currentLocation.Name == "Farm" && nextAction == NextAction.findAnimalBuildings)
            {
                autoPlayActive = true;
                animalBuildings = FindAnimalBuildings();
                if(animalBuildings.Count > 0)
                {
                    nextAction = NextAction.moveToAnimalBuildings;
                    this.Monitor.Log($"Found {animalBuildings.Count} Animal Buildings", LogLevel.Debug);
                }
                autoPlayActive = true;
            }

            // Move To Animal Building
            if (Game1.currentLocation.Name == "Farm" && nextAction == NextAction.moveToAnimalBuildings)
            {
                autoPlayActive = true;
                if (animalBuildings.Count > 0)
                {
                    destinationChecker = new Point(animalBuildings[0].getPointForHumanDoor().X, animalBuildings[0].getPointForHumanDoor().Y + 1);
                    movementDestination = new MovementDestination(destinationChecker, MovementDestination.MovementAction.EnterAnimalBuilding, NextAction.petAnimals);
                    
                    MoveToLocation(destinationChecker);
                    this.Monitor.Log($"Moving to  {animalBuildings[0].buildingType} at X: {destinationChecker.X} Y: {destinationChecker.Y}", LogLevel.Debug);
                    nextAction = NextAction.awaitDestination;
                }
            }

            // Destination Checker
            if (nextAction == NextAction.awaitDestination)
            {
                if(Game1.player.getTileX() == destinationChecker.X && Game1.player.getTileY() == destinationChecker.Y)
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

            // Pet Animals
            if ((Game1.currentLocation.Name.Contains("Barn") || Game1.currentLocation.Name.Contains("Coop")) && nextAction == NextAction.petAnimals) // destinationMonitor)
            {
                this.Monitor.Log($"Let the petting being...", LogLevel.Debug);
                nextAction = NextAction.noAction;
            }






        }






        // *****************************************************
        // AUX FUNCTIONS
        // *****************************************************
        private void MoveToLocation(Point destination)
        {
            //this.Monitor.Log($"Moving to Destination: {destination}", LogLevel.Debug);
            testPathFind = new PathFindController(Game1.player, Game1.player.currentLocation, destination, 0);
            //this.Monitor.Log($"testPathFind: {testPathFind}", LogLevel.Debug);
        }

        private List<Building> FindAnimalBuildings()
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
        
    }
}
