using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPUtils
{
    class MPStirlingEngine : PartModule
    {

        [KSPField]
        public string resourceName = null;

        [KSPField]
        public double resourceAmt = 0.001f;

        [KSPField]
        public bool actRad = true;

        [KSPField]
        public bool alwaysOn = true;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Electric Charge")]
        public double transAmt = 0.0;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || this.vessel == null || string.IsNullOrEmpty(resourceName))
            {
                return;
            }
            double kw = Math.Abs(this.part.thermalRadiationFlux);
            // use this part's vessel, not the active vessel
            Part rPart = MPFunctions.GetResourcePart(this.vessel, resourceName);
            int id = MPFunctions.GetResourceID(rPart, resourceName);
            if (rPart != null && id != -1)
            {
                double rTotal = MPFunctions.GetResourceTotal(this.vessel, resourceName);
                double rMax = MPFunctions.GetResourceMax(this.vessel, resourceName);
                transAmt = resourceAmt * kw;
                    if (actRad == true) // look for active radiator
                    {
                         for (int i = this.part.Modules.Count - 1; i >= 0; --i)
                        {
                            PartModule M = this.part.Modules[i];
                            if (M is ModuleActiveRadiator)
                            {
                                if ((M as ModuleActiveRadiator).IsCooling)
                                {
                                    rPart.TransferResource(id, transAmt);
                            }
                        }
                        }
                    }
                    else
                    {
                   rPart.TransferResource(id, transAmt);
                    }
            }
        }
    }
}
