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
        void OnGUI()
        {
            if (MPSubComputer.guiActive)
            {
                windowRect = GUI.Window(0, windowRect, DoMyWindow, "Kommodore Vic-10 Dive Computer");
            }
        }
        void DoMyWindow(int windowID)
        {
            GUI.Label(new Rect(10, 20, 280, 20), "Depth below keel: "+MPFunctions.DepthBelowKeel(FlightGlobals.ActiveVessel));
            GUI.Label(new Rect(10, 35, 280, 20), "Dive angle: "+MPFunctions.GetPitch(FlightGlobals.ActiveVessel));
            GUI.DragWindow();
        }
    }   
}
