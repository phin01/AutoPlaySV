using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPlaySV
{
    class PlayerSchedule
    {

        private Dictionary<ModEntry.Action, List<ModEntry.Action>> currentSchedule;

        public PlayerSchedule()
        {

        }


        public void setCurrentSchedule(ModEntry.Schedule scheduleMode)
        {
            currentSchedule = new Dictionary<ModEntry.Action, List<ModEntry.Action>>();

            switch (scheduleMode)
            {
                
                case ModEntry.Schedule.Rancher:

                    currentSchedule.Add(ModEntry.Action.startSchedule, new List<ModEntry.Action> { ModEntry.Action.findAnimalBuildings });
                    currentSchedule.Add(ModEntry.Action.findAnimalBuildings, new List<ModEntry.Action> { ModEntry.Action.moveToAnimalBuildings });
                    currentSchedule.Add(ModEntry.Action.moveToAnimalBuildings, new List<ModEntry.Action> { ModEntry.Action.getAnimalsList, ModEntry.Action.awaitDestination });
                    currentSchedule.Add(ModEntry.Action.getAnimalsList, new List<ModEntry.Action> { ModEntry.Action.moveToAnimal, ModEntry.Action.exitAnimalHouse });
                    currentSchedule.Add(ModEntry.Action.moveToAnimal, new List<ModEntry.Action> { ModEntry.Action.updateAnimalList, ModEntry.Action.awaitDestination });
                    currentSchedule.Add(ModEntry.Action.updateAnimalList, new List<ModEntry.Action> { ModEntry.Action.moveToAnimal, ModEntry.Action.exitAnimalHouse });
                    currentSchedule.Add(ModEntry.Action.exitAnimalHouse, new List<ModEntry.Action> { ModEntry.Action.updateAnimalBuildingList });
                    currentSchedule.Add(ModEntry.Action.updateAnimalBuildingList, new List<ModEntry.Action> { ModEntry.Action.moveToAnimalBuildings, ModEntry.Action.endSchedule });
                    break;

                default:
                    currentSchedule.Add(ModEntry.Action.noAction, new List<ModEntry.Action> { ModEntry.Action.noAction });
                    break;
            }
        }

        public ModEntry.Action GetNextAction(ModEntry.Action currentAction, bool primaryAction = true)
        {
            List<ModEntry.Action> followingActions = new List<ModEntry.Action>();

            if (currentSchedule.TryGetValue(currentAction, out followingActions))
            {
                if (primaryAction)
                    return followingActions[0];
                else
                    return followingActions[1];
            }
            else
                return ModEntry.Action.noAction;
        }








    }
}
