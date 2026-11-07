/*********************************************************************************************
* The FengistAnim plugin is copyright 2015-2018 DataInterlock, under a modified
* Creative Commons Attribution-NonCommercial 4.0 International Public License which should
* have been included with this code.
* Layered Animation code public domain by Starwaster
* Thanks to JPLRepo for helping making it more efficient
**********************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPUtils
{
    public class MPEngine : PartModule
    {


        [KSPField]
        public bool shutdownUnder;  // in or out.  shuts down the engine based on if in or out of water.

        public override void OnStart(StartState state)
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }
            MPLog.Writelog("[Maritime Pack] (MPEngine) MPEngine Found" + this.vessel.name);
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }
            if (shutdownUnder == true) //shut down a submerged engine
            {
                if (this.part.WaterContact && MPFunctions.findAltitude(this.part.transform) <= -1)
                {
                    for (int i = this.part.Modules.Count - 1; i >= 0; --i)
                    {
                        PartModule M = this.part.Modules[i];
                        if (M.isActiveAndEnabled)
                        {
                            if (M is ModuleEnginesFX)
                            {
                                    ModuleEnginesFX E = M as ModuleEnginesFX;
                                if (!E.flameout)
                                {
                                    E.Flameout("Flooded");
                                    E.Shutdown();
                                }
                            }
                            if (M is ModuleEngines)
                            {
                                ModuleEngines F = M as ModuleEngines;
                                if (!F.flameout)
                                {
                                    F.Flameout("Flooded");
                                    F.Shutdown();
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (!this.part.WaterContact)
                {
                    for (int i = this.part.Modules.Count - 1; i >= 0; --i)
                    {
                        PartModule M = this.part.Modules[i];
                        {
                            if (M is ModuleEnginesFX)
                            {
                                ModuleEnginesFX E = M as ModuleEnginesFX;
                                if (!E.flameout)
                                {
                                    E.Flameout("Engine out of the water");
                                    E.Shutdown();
                                }
                            }
                            if (M is ModuleEngines)
                            {
                                ModuleEngines F = M as ModuleEngines;
                                if (!F.flameout)
                                {
                                    F.Flameout("Engine out of the water");
                                    F.Shutdown();
                                }
                            }
                            
                        }
                    }
                }
            }
        }
    }
}

