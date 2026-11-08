/*********************************************************************************************
* The Maritime Pack MPUtil plugin is copyright 2015 Fengist, all rights reserved.
*********************************************************************************************/

using System;
using UnityEngine;

namespace MPUtils
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class MPSubGUI : MonoBehaviour
    {
        public Rect windowRect = new Rect(20, 45, 300, 60);
        // was window id 0, which collides with any other mod that also uses id 0
        private readonly int windowId = "MPUtils.MPSubGUI".GetHashCode();

        void OnDestroy()
        {
            // the flag is static: do not reopen the window on the next flight scene by itself
            MPSubComputer.guiActive = false;
        }

        void OnGUI()
        {
            if (MPSubComputer.guiActive && FlightGlobals.ActiveVessel != null)
            {
                windowRect = GUI.Window(windowId, windowRect, DoMyWindow, "Kommodore Vic-10 Dive Computer");
            }
        }
        void DoMyWindow(int windowID)
        {
            Vessel v = FlightGlobals.ActiveVessel; // null for a frame while switching vessels
            GUI.Label(new Rect(10, 20, 280, 20), "Depth below keel: "+MPFunctions.DepthBelowKeel(v));
            GUI.Label(new Rect(10, 35, 280, 20), "Dive angle: "+(v != null ? MPFunctions.GetPitch(v).ToString() : "n/a"));
            GUI.DragWindow();
        }
    }   
}
