using System;
using System.Collections.Generic;
using UnityEngine;

namespace MPHover
{
    public class MPHoverEngine : PartModule
    {
        [KSPField(guiActive = true, guiName = "Lift Thrust")]
        public int liftThrust = 0;

        [KSPField(isPersistant = true, guiActive = true, guiName = "liftEngineActive")]
        public bool liftEngineActive = false;

        [KSPEvent(guiActive = true, guiName = "Activate Lift Engine", isPersistent = true)]
        public void HEngineActivate()
        {
            Events["HEngineActivate"].active = false;
            Events["HEngineDeactivate"].active = true;
            liftEngineActive = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Lift Engine", isDefault = true, active = false, isPersistent = true)]
        public void HEngineDeactivate()
        {
            Events["HEngineActivate"].active = true;
            Events["HEngineDeactivate"].active = false;
            liftEngineActive = false;
        }

        public override void OnAwake()
        {
            Debug.Log("[FS Hover] Awake");
        }

        public override void OnStartFinished(StartState state)
        {
            Debug.Log("[FS Hover] Start Finished");
        }

        public override void OnLoad(ConfigNode node)
        {
            Debug.Log("[FS Hover] Load");
        }

        public override void OnSave(ConfigNode node)
        {
            Debug.Log("[FS Hover] Save");
        }

        public override void OnInitialize()
        {
            Debug.Log("[FS Hover] Init");

            if (HighLogic.LoadedSceneIsFlight)
            {
                var partHeightQuery = new PartHeightQuery(float.MaxValue);
                int count = this.vessel.parts.Count;
                for (int i = 0; i < count; i++)
                {
                    var p = this.vessel[i];
                    partHeightQuery.lowestOnParts.Add(p, float.MaxValue);
                    Collider[] componentsInChildren = p.GetComponentsInChildren<Collider>();
                    int num = componentsInChildren.Length;
                    for (int j = 0; j < num; j++)
                    {
                        Collider collider = componentsInChildren[j];
                        if (collider.enabled && collider.gameObject.layer != 21)
                        {
                            partHeightQuery.lowestPoint = Mathf.Min(partHeightQuery.lowestPoint, collider.bounds.min.y);
                            partHeightQuery.lowestOnParts[p] = Mathf.Min(partHeightQuery.lowestOnParts[p], collider.bounds.min.y);
                        }
                    }
                }
                count = this.vessel.parts.Count;
                for (int k = 0; k < count; k++)
                    this.vessel[k].SendMessage("OnPutToGround", partHeightQuery, SendMessageOptions.DontRequireReceiver);
                this.vessel.situation = Vessel.Situations.LANDED;
                base.vessel.Landed = true;
                base.vessel.landedAt = "";
            }
        }

        public override void OnStart(StartState state)
        {
            Debug.Log("[FS Hover] Engine Found");
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FSHoverDoLift : MonoBehaviour
    {
        public List<Transform> transformList = new List<Transform>();
        public List<Rigidbody> rbList = new List<Rigidbody>();
        public List<float> heightList = new List<float>();

        private List<Vessel> allVesselsWithHE = new List<Vessel>();
        public int lastVesselCount;
        public static int totalThrust = 0;
        private static int layermask = 1 << 15;
        private static float maxHoverDist = 1.10f;
        private float minHoverDist = 1.0f;
        private float hoverHeight = 0f;
        public AnimationCurve thrustCurve;
        public AnimationCurve thrustToNegate;


        public static float CalculateHoverHeight(Vector3 LiftPos)
        {
            float Height = 10, GD = 10, SD = 10;
            RaycastHit HoverRay;

            Vector3 geeDir = FlightGlobals.getGeeForceAtPosition(LiftPos).normalized;

            if (Physics.Raycast(LiftPos, geeDir, out HoverRay, maxHoverDist + 10, layermask))
            {
                GD = HoverRay.distance;
            }

            if (FlightGlobals.currentMainBody.ocean)
            {
                float altitude = (float)FlightGlobals.getAltitudeAtPos(LiftPos);
                Plane Hover = new Plane(-geeDir, LiftPos + altitude * geeDir);

                Ray R;
                R = new Ray(LiftPos, geeDir);

                Hover.Raycast(R, out SD);
            }

            Height = rayclamp(GD, SD);

            //airConsumed = base.part.RequestResource("CushionPressure", AirConsumption * Height);
            return Height;
        }

        private static float rayclamp(float distP, float distO)
        {
            float dist;
            dist = Math.Min(distP, distO);
            return dist;
        }

        public void onStart()
        {
        }

        public void Awake()
        {
            lastVesselCount = 0;            
        }

        public void alterHEVesselList()
        {
            allVesselsWithHE.Clear();

            foreach (Vessel aVessel in FlightGlobals.Vessels)
            {

                foreach (Part aPart in aVessel.Parts)
                {

                    foreach (PartModule aModule in aPart.Modules)
                    {
                        if (aModule is MPHoverEngine)
                        {
                            allVesselsWithHE.Add(aVessel);
                        }
                    }
                }
            }
            lastVesselCount = FlightGlobals.Vessels.Count;
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (FlightGlobals.Vessels.Count != lastVesselCount)
                {
                    alterHEVesselList();
                }
                checkHoverOn();
            }
        }

        public void checkHoverOn()
        {
            int totalThrust = 0;

            foreach (Vessel aVessel in FlightGlobals.Vessels)
            {
                totalThrust = 0;
                foreach (Part aPart in aVessel.Parts)
                {
                    foreach (PartModule aModule in aPart.Modules)
                    {
                        if (aModule is MPHoverEngine)
                        {
                            MPHoverEngine thisHoverEngine = aModule.GetComponent<MPHoverEngine>();
                            if (thisHoverEngine.liftEngineActive)
                            {
                                totalThrust += thisHoverEngine.liftThrust;
                            }
                        }
                    }
                }
                if (totalThrust > 0)
                {
                    applyHoverThrust(aVessel, totalThrust);
                }
            }
        }

        public void applyHoverThrust(Vessel affectVessel, float totalThrust)
        {

            Rigidbody rb;

            Vector3 thisWayUp = (FlightGlobals.ActiveVessel.rootPart.transform.position - FlightGlobals.ActiveVessel.mainBody.position).normalized;

            transformList.Clear();
            rbList.Clear();
            heightList.Clear();

            foreach (Part aPart in affectVessel.Parts)
            {
                foreach (PartModule aModule in aPart.Modules)
                {
                    if (aModule is MPHoverSkirt)
                    {
                        MPHoverSkirt thisHoverSkirt = aModule.GetComponent<MPHoverSkirt>();
                        for (int i = 0; i < thisHoverSkirt.transformList.Count; i++)
                        {
                            transformList.Add(thisHoverSkirt.transformList[i]);
                            rbList.Add(thisHoverSkirt.rbList[i]);
                            heightList.Add(thisHoverSkirt.heightList[i]);
                        }
                    }
                }
            }

            //Debug.Log(affectVessel.name);

            float thrustToAdd = 0f;
            float totalHeight = 0f;
            for (int i = 0; i < transformList.Count; i++)
            {
                totalHeight += CalculateHoverHeight(transformList[i].position);
            }
            float aveHeight = totalHeight / transformList.Count;
            for (int i = 0; i < transformList.Count; i++)
            {
                rb = rbList[i];
                rb.drag = 0.12f;
                hoverHeight = CalculateHoverHeight(transformList[i].position);
                //Debug.Log("HH :" + hoverHeight);
                ////how much height did we gain or lose?
                var heightChange = Math.Round(hoverHeight - heightList[i], 6);
                //Debug.Log("HC :" + heightChange);
                ////where do we need to be?
                var optimalHeight = Math.Round(((maxHoverDist - minHoverDist) / 2) + minHoverDist, 6);
                ////how are we getting there?
                var heightNeeded = Math.Round(optimalHeight - hoverHeight, 6);
                //Debug.Log("HN :" + heightNeeded);
                ////thrust needed

                thrustCurve = AnimationCurve.EaseInOut(0f, 1f, maxHoverDist, 0.0f);
                thrustCurve.AddKey(new Keyframe(maxHoverDist * 0.8f, 0.9f));
                thrustToAdd = thrustCurve.Evaluate(hoverHeight) * (totalThrust / transformList.Count);
                //Debug.Log("Eval :" + thrustCurve.Evaluate(hoverHeight));

                thrustToNegate = AnimationCurve.EaseInOut(-0.001f, -1, 0.001f, 1);
                var reverseThrust = thrustToNegate.Evaluate((float)heightChange);
                if (hoverHeight < maxHoverDist)
                {
                    rb.AddForceAtPosition((thisWayUp.normalized) * (reverseThrust * -1), transformList[i].position, ForceMode.Impulse);
                }
                //Debug.Log("Adding Reverse Thrust: " + reverseThrust + ":" + transformList[i].position + ":" + transformList[i].name);

                
                rb.AddForceAtPosition((thisWayUp.normalized) * thrustToAdd, transformList[i].position, ForceMode.Impulse);
                //Debug.Log("Adding Thrust: " + thrustToAdd + " Position :" + transformList[i].position + " Transform :" +transformList[i].name + " Height :" +hoverHeight);
            }
        }
    }


    public class MPHoverSkirt : PartModule
    {

        [KSPField(isPersistant = true)]
        public double aveAlt = 0;

        public List<Transform> transformList = new List<Transform>();
        public List<Rigidbody> rbList = new List<Rigidbody>();
        public List<float> heightList = new List<float>();

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                float totalHeight = 0;
                for (int i = 0; i < transformList.Count; i++)
                {
                    var hoverheight = FSHoverDoLift.CalculateHoverHeight(transformList[i].position);
                    totalHeight += hoverheight;
                    heightList[i] = hoverheight;
                }
                aveAlt = totalHeight / transformList.Count;
            }
        }


        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                Rigidbody rb;

//                Debug.Log("[FS Hover] Skirt Awake");
                rbList.Clear();
                transformList.Clear();
                heightList.Clear();

                rb = this.part.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = this.part.gameObject.AddComponent<Rigidbody>();
                }

                if (this.part.FindModelTransform("portLift"))
                {
                    transformList.Add(this.part.FindModelTransform("portLift"));
                    rbList.Add(rb);
                    heightList.Add(0);
                }
                if (this.part.FindModelTransform("starboardLift"))
                {
                    transformList.Add(this.part.FindModelTransform("starboardLift"));
                    rbList.Add(rb);
                    heightList.Add(0);
                }
                if (this.part.FindModelTransform("centerLift"))
                {
                    transformList.Add(this.part.FindModelTransform("centerLift"));
                    rbList.Add(rb);
                    heightList.Add(0);
                }
            }
        }
    }
}

