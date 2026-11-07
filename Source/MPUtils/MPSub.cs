/*********************************************************************************************
* The Maritime Pack MPUtil plugin is copyright 2015 Fengist, all rights reserved.
* For full license information please visit http://www.kerbaltopia.com
*********************************************************************************************/


using UnityEngine;

namespace MPUtils
{
    public class MPSub : PartModule
    {
        [KSPField]
        public bool manageIntakes = false;

        [KSPField]
        public string pressureResource1 = "";

        [KSPField]
        public double pressureResource1Amount = 0.0f;

        [KSPField]
        public string pressureResource2 = "";

        [KSPField]
        public double pressureResource2Amount = 0.0f;

        [KSPField]
        public string depressureResource1 = "";

        [KSPField]
        public double depressureResource1Amount = 0.0f;

        [KSPField]
        public string depressureResource2 = "";

        [KSPField]
        public double depressureResource2Amount = 0.0f;


        [KSPField(isPersistant = true)]
        double currentPressure = 0;

        [KSPField(isPersistant = true, guiName = "Max Depth", guiUnits = "m", guiFormat = "F0",
    guiActiveEditor = true), UI_FloatRange(scene = UI_Scene.Editor, minValue = 400f, maxValue = 2000f, stepIncrement = 10f)]
        public float MaxDepth = 400f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Current Pressure")]
        public string sPressure = "0.00 kPa";

        public bool pressurizerActive = false;
        public bool depressurizerActive = false;

        [KSPEvent(guiActive = true, guiName = "Activate Pressurizer")]
        public void PressurizerActivate()
        {
            pressurizerActive = true;
            depressurizerActive = false;
            pumpStatus = "Pressurizing";
            Events["PressurizerActivate"].active = false;
            Events["PressurizerDeactivate"].active = true;
            Events["DePressurizerActivate"].active = true;
            Events["DePressurizerDeactivate"].active = false;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Pressurizer", active = false)]
        public void PressurizerDeactivate()
        {
            pressurizerActive = false;
            pumpStatus = "Off";
            Events["PressurizerActivate"].active = true;
            Events["PressurizerDeactivate"].active = false;
        }

        [KSPEvent(guiActive = true, guiName = "Activate Depressurizer")]
        public void DePressurizerActivate()
        {
            depressurizerActive = true;
            pressurizerActive = false;
            pumpStatus = "Depressurizing";
            Events["DePressurizerActivate"].active = false;
            Events["DePressurizerDeactivate"].active = true;
            Events["PressurizerActivate"].active = true;
            Events["PressurizerDeactivate"].active = false;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Depressurizer", active = false)]
        public void DePressurizerDeactivate()
        {
            depressurizerActive = false;
            pumpStatus = "Off";
            Events["DePressurizerActivate"].active = true;
            Events["DePressurizerDeactivate"].active = false;
        }

        [KSPField(isPersistant = false, guiActive = true, guiName = "Pump Status")]
        public string pumpStatus = "Off";

        [KSPField(isPersistant = false, guiActive = true, guiName = "MaxPressure Pressure")]
        public string maxPressure = "50000";

        bool intakesOpen = true;

        private double pumpDelay = 0.0f;

        private double messageDelay = 0.0f;

        public override void OnStart(StartState state)
        {

            MPLog.Writelog("[Maritime Pack] MPSub Found");

            if (HighLogic.LoadedSceneIsFlight)
            {
                // set the default pressure to all parts
                if (currentPressure <= 0.0f)
                {
                    currentPressure = 4000f;
                }
                for (int i = this.vessel.parts.Count - 1; i >= 0; --i)
                {
                    this.vessel.parts[i].maxPressure = currentPressure;
                }
                /*  This code just double checks that part maxPressure is set
                double mPress = double.Parse(maxPressure);
                for (int i = this.vessel.parts.Count - 1; i >= 0; --i)
                {
                    double thisp = this.vessel.parts[i].maxPressure;
                    if (thisp < mPress)
                    {
                        mPress = thisp;
                    }
                }
                */
                maxPressure = currentPressure + " kPa";
            }
        }

        public bool ConsumeResource (string thisresource, double amount)
        {
            Part rPart = MPFunctions.GetResourcePart(FlightGlobals.ActiveVessel, thisresource);
            double rTotal = MPFunctions.GetResourceTotal(FlightGlobals.ActiveVessel, pressureResource1);
            if (rPart != null && rTotal >= amount)
            {
                int id = MPFunctions.GetResourceID(rPart, thisresource);
                double rFlow = rPart.RequestResource(id, amount, ResourceFlowMode.ALL_VESSEL);
                if (rFlow > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }

            //man the pumps
            pumpDelay -= Time.deltaTime;
            messageDelay -= Time.deltaTime;
            bool letsgo = true;
            if (pumpDelay <= 0)
            {
                pumpDelay = 1;
                if (pressurizerActive == true)
                {
                    depressurizerActive = false;
                    if (currentPressure >= MaxDepth * 10)
                    {
                        pressurizerActive = false;
                     }
                    else
                    {
                        if (pressureResource1 !="")
                        {
                            letsgo = ConsumeResource(pressureResource1, pressureResource1Amount);
                        }
                        if (pressureResource2 != "" && letsgo == true)
                        {
                            letsgo = ConsumeResource(pressureResource2, pressureResource2Amount);
                        }
                        if (letsgo)
                        {
                            currentPressure += 10;
                            for (int i = this.vessel.parts.Count - 1; i >= 0; --i)
                            {
                                this.vessel.parts[i].maxPressure = currentPressure;
                            }
                        }
                    }
                    maxPressure = currentPressure + " kPa";
                }
                if (depressurizerActive == true)
                {
                    if (currentPressure <= 4000)
                    {
                        depressurizerActive = false;
                    }
                    else
                    {
                        if (depressureResource1 != "")
                        {
                            letsgo = ConsumeResource(depressureResource1, depressureResource1Amount);
                        }
                        if (depressureResource2 != "" && letsgo == true)
                        {
                            letsgo = ConsumeResource(depressureResource2, depressureResource2Amount);
                        }
                        if (letsgo)
                        {
                            currentPressure -= 10;
                            for (int i = this.vessel.parts.Count - 1; i >= 0; --i)
                            {
                                this.vessel.parts[i].maxPressure = currentPressure;
                            }
                        }
                    }
                    maxPressure = currentPressure + " kPa";
                }
            }


            //get the current pressure on the parts
            double maxSPressure = 0.0f;
            for (int i = this.vessel.parts.Count - 1; i >= 0; --i)
            {
                double thisp = this.vessel.parts[i].staticPressureAtm;
                if (thisp >maxSPressure)
                {
                    maxSPressure = thisp;
                }
                sPressure = string.Format("{0:0.00}", (maxSPressure * 100)) + " kPa";
            }

            //check for overpressure
            if (currentPressure - maxSPressure < 4000)
            {
                if (messageDelay <= 0)
                {
                    messageDelay = 5;
                    //ScreenMessages.PostScreenMessage("Warning: Vessel is Over-Pressurized!!", 3);
                }
            }



            //MaxDepth - Prevent diving below max depth
            //if (this.vessel.altitude < -MPConfig.MPMaxDepth && MPConfig.MPMaxDepth != 0)
            //{
            //    vessel.ChangeWorldVelocity(vessel.upAxis + new Vector3d(0, 0, 0.1));
            //}

            //Close Intakes if we're diving
            if (manageIntakes && vessel.isActiveVessel && this.vessel.mainBody.ocean)
            {
                if (this.vessel.altitude <= -1.0)
                {
                    if (intakesOpen)
                    {
                        for (int p = this.vessel.Parts.Count - 1; p >= 0; --p)
                        {
                            for (int i = this.vessel.Parts[p].Modules.Count - 1; i >= 0; --i)
                            {
                                PartModule m = this.vessel.Parts[p].Modules[i];
                                if (m is ModuleResourceIntake)
                                {
                                    (m as ModuleResourceIntake).enabled = false;
                                    (m as ModuleResourceIntake).Deactivate();
                                    intakesOpen = false;
                                    MPLog.Writelog("[Maritime Pack] Closing Intakes");
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (!intakesOpen)
                    {
                        for (int p = this.vessel.Parts.Count - 1; p >= 0; --p)
                        {
                            for (int i = this.vessel.Parts[p].Modules.Count - 1; i >= 0; --i)
                            {
                                PartModule m = this.vessel.Parts[p].Modules[i];
                                if (m is ModuleResourceIntake)
                                {
                                    (m as ModuleResourceIntake).enabled = true;
                                    (m as ModuleResourceIntake).Activate();
                                    intakesOpen = true;
                                    MPLog.Writelog("[Maritime Pack] Opening Intakes");
                                }
                            }
                        }
                    }
                }
            }
            //end Close Intakes
            //Engine Shutdown if we're trying to fly
            //if (manageThrottle && vessel.isActiveVessel && this.vessel.mainBody.ocean)
            //{
            //    if (this.vessel.altitude > 2.0)
            //    {
            //        FlightInputHandler.state.mainThrottle = 0.0f;
            //    }
            //}
            //end Engine Shutdown
        }
    }
}
