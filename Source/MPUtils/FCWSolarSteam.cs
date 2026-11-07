using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPUtils
{
    class FCWModuleSolarSteam : PartModule  //consumes compressed water to generate steam.
    {
        [KSPField]
        private double CWRate = 0.001f;
        private double elRate = 0.25f;

        public override void OnStart(StartState state)
        {
            if (this.vessel != null) // vessel is null in the editor
                MPLog.Writelog("[Maritime Pack] (SolarSteam) FCWSolarSteam Found on " + this.vessel.name);
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || this.vessel == null)
            {
                return;
            }
            for (int i = this.part.Modules.Count - 1; i >= 0; --i)
            {
                PartModule M = this.part.Modules[i];
                if (M is ModuleDeployableSolarPanel)
                {
                    if ((M as ModuleDeployableSolarPanel).deployState == ModuleDeployableSolarPanel.DeployState.EXTENDED)
                    {
                        Double CWTotal = MPFunctions.GetResourceTotal(this.vessel, "CompressedWater");
                        Double elecTotal = MPFunctions.GetResourceTotal(this.vessel, "ElectricCharge");
                        if (CWTotal <= 0 || elecTotal <= 0)
                        {
                            if (CWTotal <= 0)
                            {
                                (M as ModuleDeployableSolarPanel).status = "No Compressed Water";
                            }
                            else
                            {
                                (M as ModuleDeployableSolarPanel).status = "No Electric Charge";
                            }
                            (M as ModuleDeployableSolarPanel).enabled = false;
                        }
                        else
                        {
                            (M as ModuleDeployableSolarPanel).enabled = true;
                            float thisflow = (M as ModuleDeployableSolarPanel).flowRate;
                            if (thisflow > 0)
                            {
                                // use this vessel (was the active vessel, which may not carry the resources at all)
                                Part sPart = MPFunctions.GetResourcePart(this.vessel, "CompressedWater");
                                int CWID = MPFunctions.GetResourceID(sPart, "CompressedWater");
                                if (sPart == null || CWID == -1) return;
                                double CWFlow = sPart.RequestResource(CWID, thisflow * CWRate, ResourceFlowMode.ALL_VESSEL);
                                sPart = MPFunctions.GetResourcePart(this.vessel, "ElectricCharge");
                                int EcID = MPFunctions.GetResourceID(sPart, "ElectricCharge");
                                if (sPart == null || EcID == -1) return;
                                sPart.RequestResource(EcID, CWFlow * elRate, ResourceFlowMode.ALL_VESSEL);
                            }
                        }
                    }
                    else
                    {
                        (M as ModuleDeployableSolarPanel).enabled = true;
                    }
                }
            }
        }
    }
}

