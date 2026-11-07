using UnityEngine;

namespace MPUtils
{
    class MPResDecay : PartModule
    {

        [KSPField]
        public string resourceName = "null";  

        [KSPField]
        public float decayPeriod = 0.0f;  

        [KSPField]
        public float decayAmount = 0.0f;  

        private Part resPart = null;
        private int resID = -1;
        private double resAmt = 0;
        private float varPeriod = 0;

        public override void OnStart(StartState state)
        {
            varPeriod = decayPeriod;
        }

        public void FixedUpdate()
        {
            // was: ran in the editor too, where FlightGlobals.ActiveVessel is null -> NullReferenceException
            if (!HighLogic.LoadedSceneIsFlight || this.vessel == null)
            {
                return;
            }
            varPeriod -= Time.deltaTime;
            if (varPeriod < 0)
            {
                varPeriod = decayPeriod;
                resPart = MPFunctions.GetResourcePart(this.vessel, resourceName);
                resID = MPFunctions.GetResourceID(resPart, resourceName);
                resAmt = MPFunctions.GetResourceTotal(this.vessel, resourceName);
                if (resPart != null && resAmt > 0 && resID != -1)
                {
                    try
                    {
                        resPart.RequestResource(resID, decayAmount, ResourceFlowMode.ALL_VESSEL);
                    }
                    catch
                    {
                        MPLog.Writelog("[Maritime Pack] ResDecay: Error removing resource");
                    }
                }
            }
        }
    }
}
