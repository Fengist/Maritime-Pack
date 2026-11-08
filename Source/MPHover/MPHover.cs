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

            // vessel can be null when the part is initialised outside a fully loaded vessel
            if (HighLogic.LoadedSceneIsFlight && this.vessel != null && this.vessel.parts != null)
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
        private bool curvesBuilt = false;


        public static float CalculateHoverHeight(Vector3 LiftPos)
        {
            float Height = 10, GD = 10, SD = 10;
            RaycastHit HoverRay;

            if (FlightGlobals.currentMainBody == null)
                return Height;

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

        private bool vesselListDirty = true;

        public void Awake()
        {
            lastVesselCount = 0;
        }

        public void Start()
        {
            // FlightGlobals.Vessels.Count does not change when a vessel comes into physics range or is
            // docked/undocked/split, so also rebuild the list on these events.
            GameEvents.onVesselLoaded.Add(OnVesselListChanged);
            GameEvents.onVesselCreate.Add(OnVesselListChanged);
            GameEvents.onVesselWasModified.Add(OnVesselListChanged);
            GameEvents.onVesselDestroy.Add(OnVesselListChanged);
        }

        public void OnDestroy()
        {
            GameEvents.onVesselLoaded.Remove(OnVesselListChanged);
            GameEvents.onVesselCreate.Remove(OnVesselListChanged);
            GameEvents.onVesselWasModified.Remove(OnVesselListChanged);
            GameEvents.onVesselDestroy.Remove(OnVesselListChanged);
        }

        private void OnVesselListChanged(Vessel v)
        {
            vesselListDirty = true;
        }

        public void alterHEVesselList()
        {
            allVesselsWithHE.Clear();

            foreach (Vessel aVessel in FlightGlobals.Vessels)
            {
                if (aVessel == null || !aVessel.loaded || aVessel.Parts == null)
                    continue;

                bool found = false;
                foreach (Part aPart in aVessel.Parts)
                {
                    if (aPart == null)
                        continue;
                    foreach (PartModule aModule in aPart.Modules)
                    {
                        if (aModule is MPHoverEngine)
                        {
                            // was: added once per hover engine, so a craft with two engines was listed twice
                            allVesselsWithHE.Add(aVessel);
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        break;
                }
            }
            lastVesselCount = FlightGlobals.Vessels.Count;
            vesselListDirty = false;
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && FlightGlobals.Vessels != null)
            {
                if (vesselListDirty || FlightGlobals.Vessels.Count != lastVesselCount)
                {
                    alterHEVesselList();
                }
                checkHoverOn();
            }
        }

        public void checkHoverOn()
        {
            int totalThrust = 0;

            // was: every part module of every vessel in the game, every physics frame. The list built by
            // alterHEVesselList (previously unused) only holds loaded vessels with a hover engine.
            for (int v = 0; v < allVesselsWithHE.Count; v++)
            {
                Vessel aVessel = allVesselsWithHE[v];
                if (aVessel == null || !aVessel.loaded || aVessel.packed || aVessel.Parts == null)
                    continue;
                totalThrust = 0;
                foreach (Part aPart in aVessel.Parts)
                {
                    if (aPart == null)
                        continue;
                    foreach (PartModule aModule in aPart.Modules)
                    {
                        MPHoverEngine thisHoverEngine = aModule as MPHoverEngine;
                        if (thisHoverEngine != null && thisHoverEngine.liftEngineActive)
                        {
                            totalThrust += thisHoverEngine.liftThrust;
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

            // was: FlightGlobals.ActiveVessel.rootPart / mainBody - NullReferenceException whenever there is no
            // active vessel for a frame (vessel switch, active vessel destroyed), and the wrong "up" for any
            // hovercraft that is not the active vessel.
            if (affectVessel == null || affectVessel.mainBody == null || affectVessel.Parts == null)
                return;
            Vector3 thisWayUp = (affectVessel.transform.position - affectVessel.mainBody.position).normalized;

            transformList.Clear();
            rbList.Clear();
            heightList.Clear();

            foreach (Part aPart in affectVessel.Parts)
            {
                if (aPart == null)
                    continue;
                foreach (PartModule aModule in aPart.Modules)
                {
                    MPHoverSkirt thisHoverSkirt = aModule as MPHoverSkirt;
                    if (thisHoverSkirt != null)
                    {
                        // prefer the part's live rigidbody over the one cached in OnStart
                        Rigidbody skirtRb = aPart.rb;
                        for (int i = 0; i < thisHoverSkirt.transformList.Count; i++)
                        {
                            Rigidbody r = skirtRb != null ? skirtRb : thisHoverSkirt.rbList[i];
                            if (thisHoverSkirt.transformList[i] == null || r == null)
                                continue;
                            transformList.Add(thisHoverSkirt.transformList[i]);
                            rbList.Add(r);
                            heightList.Add(thisHoverSkirt.heightList[i]);
                        }
                    }
                }
            }

            //Debug.Log(affectVessel.name);

            if (transformList.Count == 0)
                return; // no usable skirts: nothing to push on (and avoids dividing by zero below)

            float thrustToAdd = 0f;
            float totalHeight = 0f;
            for (int i = 0; i < transformList.Count; i++)
            {
                totalHeight += CalculateHoverHeight(transformList[i].position);
            }
            float aveHeight = totalHeight / transformList.Count;
            // Both curves are constant. They used to be rebuilt (two AnimationCurve allocations) for every
            // lift point on every physics frame.
            if (!curvesBuilt)
            {
                thrustCurve = AnimationCurve.EaseInOut(0f, 1f, maxHoverDist, 0.0f);
                thrustCurve.AddKey(new Keyframe(maxHoverDist * 0.8f, 0.9f));
                thrustToNegate = AnimationCurve.EaseInOut(-0.001f, -1, 0.001f, 1);
                curvesBuilt = true;
            }
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

                thrustToAdd = thrustCurve.Evaluate(hoverHeight) * (totalThrust / transformList.Count);
                //Debug.Log("Eval :" + thrustCurve.Evaluate(hoverHeight));

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
                    if (transformList[i] == null)
                        continue;
                    var hoverheight = FSHoverDoLift.CalculateHoverHeight(transformList[i].position);
                    totalHeight += hoverheight;
                    heightList[i] = hoverheight;
                }
                if (transformList.Count > 0)
                    aveAlt = totalHeight / transformList.Count; // was NaN (persisted to the save) without lift transforms
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

