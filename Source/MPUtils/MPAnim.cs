/*********************************************************************************************
* The Maritime Pack MPUtil plugin is copyright 2015 Fengist, all rights reserved.
* For more information please visit http://www.kerbaltopia.com
*
* Layered Animation code public domain by Starwaster
* Thanks to JPLRepo for helping making the MPanimEngine class more efficient
**********************************************************************************************/

using System;
using System.Linq;
using UnityEngine;

namespace MPUtils
{

    public class ModuleLayeredAnimator : ModuleAnimateGeneric
    {
        [KSPField]
        public int layer = 1;

        public ModuleLayeredAnimator()
        {
        }

        public override void OnStart(StartState state)
        {
            this.anim[this.animationName].layer = layer;
        }
    }

    public class MPanimEngine : PartModule
    {
        [KSPField]
        public string animationName = "null";  //the name of the animation stored in the .cfg

        [KSPField]
        public string partType = "null";  //the part type that it's connected to (Engine / Intake)

        [KSPField]
        public bool syncThrust = false;  //is the animation synced to the engine thrust

        [KSPField]
        public bool syncThrottle = false;  //is the animation synced to the throttle position

        [KSPField]
        public bool smoothThrottle = false; //should smoothing be used when the throttle position changes rapidly

        [KSPField]
        public bool loopAnim = false; //should smoothing be used when the throttle position changes rapidly

        [KSPField]
        public float animSpeed = 1.0f; //how fast an animation plays

        [KSPField]
        public bool detectReverseThrust = false; // should we detect the reversing of the thrust transform

        [KSPField]
        public bool smoothRev = true; //should we switch smootly into reverse animation

        [KSPField]
        public float revDelay = 1.0f; //the delay time to reverse the animation

        [KSPField]
        public bool useRotorDiscSwap = false;
        [KSPField]
        public string rotorDiscName = "rotorDisc";
        [KSPField]
        public float rotorDiscFadeInStart = 0.1f;
        [KSPField]
        public string propellerName = "propeller";

        private Transform rotorDisc;
        private Transform propeller;

        private double logdelay = 0.0f;
        public Animation aniEngine = null; //the animation
        private ModuleEngines myEngine = new ModuleEngines();  //the first or default engine found in the part
        private Transform mytransform;
        private ModuleResourceIntake myIntake = new ModuleResourceIntake();
        private float lastThrottle = 0;  //tracks the last throttle position. Always 0 or 1 for non sync'd engines
        private Quaternion startRotation = new Quaternion(0, 0, 0, 0);
        private float lastdirection = 1.0f;
        
        public void Play_Anim(string aname, float aspeed, float atime = 0.0f)
        {
            logdelay -= Time.deltaTime;
            if (useRotorDiscSwap == true)
            {
                if (propeller != null && rotorDisc != null)
                {
                    if (Math.Abs(aspeed) >= rotorDiscFadeInStart)
                    {
                        rotorDisc.gameObject.GetComponent<Renderer>().enabled = true;
                        propeller.gameObject.GetComponent<Renderer>().enabled = false;

                    }
                    else
                    {
                        rotorDisc.gameObject.GetComponent<Renderer>().enabled = false;
                        propeller.gameObject.GetComponent<Renderer>().enabled = true;
                    }
                }
            }
            try
            {
                Animation anim;
                Animation[] animators = part.FindModelAnimators(aname);
                if (animators.Length > 0)
                {
                    anim = animators[0];
                    anim.clip = anim.GetClip(animationName);
                    if (loopAnim == true)
                    {
                        anim[aname].speed = aspeed * animSpeed;
                        anim[aname].wrapMode = WrapMode.Loop;
                    }
                    else
                    {
                        anim[aname].speed = aspeed;
                        anim[aname].normalizedTime = atime;
                    }
                    if (logdelay <= 0)
                    {
                        //MPLog.Writelog("[Maritime Pack] (aniEngine) Playing: "+aname+" speed:"+aspeed+" time:"+atime);
                        logdelay = 3;
                    }
                    anim.Play(aname);
                }
            }
            catch (Exception ex)
            {
                MPLog.Writelog("[Maritime Pack] (aniEngine) Exception in Play_Anim");
                MPLog.Writelog("[Maritime Pack] (aniEngine) Err: " + ex);
            }
        }

        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            {
                if (partType == "Engine")
                {
                    myEngine = part.Modules.OfType<ModuleEngines>().FirstOrDefault();
                    mytransform = part.FindModelTransform(myEngine.thrustVectorTransformName);
                    startRotation = mytransform.localRotation;
                    if (useRotorDiscSwap == true)
                    {
                        MPLog.Writelog("[Maritime Pack] (aniEngine) Disc swapping active.");
                        propeller = part.FindModelTransform(propellerName);
                        rotorDisc = part.FindModelTransform(rotorDiscName);
                        if (propeller == null)
                        {
                            useRotorDiscSwap = false;
                            MPLog.Writelog("[Maritime Pack] (aniEngine) Rotor Disc Swap enalbled but propeller gameObject not found");
                        }
                        if (rotorDisc == null)
                        {
                            useRotorDiscSwap = false;
                            MPLog.Writelog("[Maritime Pack] (aniEngine) Rotor Disc Swap enalbled but rotorDisc gameObject not found");
                        }
                        if (rotorDisc != null)
                        {
                            try
                            {
                                rotorDisc.gameObject.GetComponent<Renderer>().enabled = false;
                            }
                            catch (Exception)
                            {
                                MPLog.Writelog("[Maritime Pack] (aniEngine) Mesh Renederer not found on rotorDisc!!!");
                            }
                        }
                        if (propeller != null)
                        {
                            try
                            {
                                propeller.gameObject.GetComponent<Renderer>().enabled = true;
                            }
                            catch (Exception)
                            {
                                MPLog.Writelog("[Maritime Pack] (aniEngine) Mesh Renederer not found on propeller!!!");
                            }
                        }
                    }
                }
                if (HighLogic.LoadedSceneIsFlight)
                {
                    foreach (var anim in part.FindModelAnimators(animationName))
                    {
                        aniEngine = anim;
                        if (aniEngine.GetClipCount() > 1)
                        {
                            aniEngine.Stop(); //stopping any animations loaded by ModuleAnimateGeneric
                        }
                        MPLog.Writelog("[Maritime Pack] (aniEngine) Found animation: " + animationName + " on " + part.name);
                    }
                    //MPLog.Writelog("[Maritime Pack] (aniEngine) Found Engine: " + myEngine.name);
                    //MPLog.Writelog("[Maritime Pack] (aniEngine) Found aniEngine Name: " + aniEngine.name);
                    //MPLog.Writelog("[Maritime Pack] (aniEngine) Sync Throttle: " + syncThrottle);
                    //MPLog.Writelog("[Maritime Pack] (aniEngine) Smooth Throttle: " + smoothThrottle);
                    //MPLog.Writelog("[Maritime Pack] (aniEngine) Loop Anim : " + loopAnim);
                    //MPLog.Writelog("[Maritime Pack] (aniEngine) Anim Speed : " + animSpeed);
                }
                if (partType == "Intake")
                {
                    myIntake = part.Modules.OfType<ModuleResourceIntake>().FirstOrDefault();
                }
            }
        }

        private float CheckDirection()
        {
            float newdirection = 1.0f;
            if (detectReverseThrust == true)
            {
                if (startRotation.x != mytransform.localRotation.x)
                {
                    newdirection = -1;
                }
                if (newdirection != lastdirection) //engines have been reversed
                {
                    if (smoothRev == true)
                    {
                        if (newdirection == -1)
                        {
                            lastdirection -= revDelay;
                        }
                        else
                        {
                            lastdirection += revDelay;
                        }
                        if (lastdirection > 1)
                        {
                            lastdirection = 1;
                        }
                        if (lastdirection < -1)
                        {
                            lastdirection = -1;
                        }
                    }
                    else // not smoothing so make the change
                    {
                        lastdirection = newdirection;
                    }
                }
            }
            return lastdirection;
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight ) { return; }
            if (myEngine == null && myIntake == null && partType != "Throttle" && partType != "Stirling" && partType != "Battery" || animationName == "null" || !vessel.isActiveVessel)
            {
                return;
            }
            else if (vessel.isActiveVessel)
            {

                double thisenergy = 0;
                if (partType == "Stirling")
                {
                    thisenergy = Math.Abs(this.part.thermalRadiationFlux) / 100;
                    if (thisenergy > 1)
                    {
                        thisenergy = 1;
                    }
                    if (thisenergy < 0)
                    {
                        thisenergy = 0;
                    }
                    Play_Anim(animationName, (float)thisenergy, 0.0f);
                    return;
                }
                if(partType == "Battery")
                {
                    double rTotal = MPFunctions.GetResourceTotal(FlightGlobals.ActiveVessel, "ElectricCharge");
                    double rMax = MPFunctions.GetResourceMax(FlightGlobals.ActiveVessel, "ElectricCharge");
                    thisenergy = rTotal / rMax;
                 }
                else
                {
                    thisenergy = FlightInputHandler.state.mainThrottle;
                }

                if ((partType == "Engine" && !myEngine.EngineIgnited) || (partType == "Engine" && myEngine.getFlameoutState == true) || (partType == "Intake" && !myIntake.intakeEnabled)) //Intake or Engine shut down, reverse animation
                {
                    if (syncThrust)
                    {
                        if (syncThrust)
                        {
                            float direction = CheckDirection();
                            thisenergy = myEngine.normalizedThrustOutput;
                            Play_Anim(animationName, (float)thisenergy * direction, 0.0f);
                            return;
                        }
                    }
                    if (lastThrottle != 0.0f)
                    {                        
                        if (syncThrottle)
                        {
                            if (smoothThrottle)
                            {
                                lastThrottle = lastThrottle - 0.02f;
                                if (lastThrottle < 0.0f)
                                {
                                    lastThrottle = 0.0f;
                                }
                            }
                            else
                            {
                                lastThrottle = 0.0f;
                            }
                            if (loopAnim == true)
                            {
                                Play_Anim(animationName, lastThrottle);
                            }
                            else
                            {
                                Play_Anim(animationName, 0.0f, lastThrottle);
                            }
                        }
                        else //not synced to play at full speed from the end to the beginning
                        {
                            if (!aniEngine.isPlaying)
                            {
                                if (loopAnim == true)
                                {
                                    Play_Anim(animationName, 0.0f);
                                }
                                else
                                {
                                    Play_Anim(animationName, -1.0f, 1.0f);
                                }
                                lastThrottle = 0.0f;
                            }
                        }
                    }
                }
                else //engine ignited or it's another type, run animation
                {
                    if (syncThrust)
                    {
                        float direction = CheckDirection();
                        thisenergy = myEngine.normalizedThrustOutput;
                        if (loopAnim == true)
                        {
                            Play_Anim(animationName, (float)thisenergy, 0.0f);
                        }
                        else
                        {
                            Play_Anim(animationName, 0.0f, (float)thisenergy);
                        }
                       return;
                    }
                    if (syncThrottle)
                    {
                        if (smoothThrottle)
                        {
                            if (thisenergy > lastThrottle)
                            {
                                if (thisenergy + lastThrottle > 0.02f)
                                {
                                    lastThrottle = lastThrottle + 0.02f;
                                    if (lastThrottle > 1.0f)
                                    {
                                        lastThrottle = 1.0f;
                                    }
                                }
                                else
                                {
                                    lastThrottle = (float) thisenergy;
                                }
                            }
                            if (thisenergy < lastThrottle)
                            {
                                if (lastThrottle - thisenergy > 0.02f || thisenergy == 0 && lastThrottle != 0)
                                {
                                    lastThrottle = lastThrottle - 0.02f;
                                    if (lastThrottle < 0.0f)
                                    {
                                        lastThrottle = 0.0f;
                                    }
                                }
                                else
                                {
                                    lastThrottle = (float) thisenergy;
                                }
                            }
                            if (loopAnim == true)
                            {
                                Play_Anim(animationName, lastThrottle, 0.0f);
                            }
                            else
                            {
                                Play_Anim(animationName, 0.0f, lastThrottle);
                            }
                        }
                        else
                        {
                            if (loopAnim == true)
                            {
                                Play_Anim(animationName, (float)thisenergy);
                            }
                            else
                            {
                                Play_Anim(animationName, 0.0f, (float)thisenergy);
                            }
                        }
                    }
                    else //not synced so play at full speed from the beginning
                    {
                        if (!aniEngine.isPlaying && lastThrottle != 1.0f)
                        {
                            Play_Anim(animationName,1.0f,0.0f);
                            lastThrottle = 1.0f;
                        }
                    }
                }
            }
        }
    }
}

