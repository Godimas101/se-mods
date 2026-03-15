using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI;
using VRage.Utils;

/*
 Fix for mod uninstall 
 If I use this <Script>AdvancedStats</Script> it will be hardcoded to save game
 and if it is not present (mod got removed) game with crash with nullpointer.
 
 This is to prevent the nullpointer, code from AdvancedStats.cs does the rest.
 If character dies the <Script>AdvancedStats</Script> will be removed automatically.
 */


namespace APEX.Advanced.Client.MyAdvancedStat
{
    // This attribute tells the game: "I am the script named 'AdvancedStats'".
    // This is the critical part that prevents the game from crashing.
    [MyStatLogicDescriptor("AdvancedStats")]
    public class MyAdvancedStatEffect : MyStatLogic
    {
        // The Init method is overridden but does absolutely nothing.
        public override void Init(IMyCharacter character, Dictionary<MyStringHash, MyEntityStat> stats, string scriptName)
        {
            // Base call is good practice, but the method is otherwise empty.
            base.Init(character, stats, scriptName);
        }

        // The Update100 method is overridden but is completely empty.
        // No logic will be executed.
        public override void Update100()
        {
            // Do nothing.
        }

        // The Close method is overridden but is completely empty.
        public override void Close()
        {
            // Do nothing.
        }
    }
}