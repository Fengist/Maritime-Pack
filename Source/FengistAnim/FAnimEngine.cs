using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FengistAnim;

public class FAnimEngine : PartModule
{
	[KSPField]
	public string animationName = "null";

	[KSPField]
	public string partType = "null";

	[KSPField]
	public bool syncThrust = false;

	[KSPField]
	public bool syncThrottle = false;

	[KSPField]
	public bool smoothThrottle = false;

	[KSPField]
	public bool loopAnim = false;

	[KSPField]
	public float animSpeed = 1f;

	[KSPField]
	public float minSpeed = 0f;

	[KSPField]
	public float spoolTime = 0f;

	private float inactiveCheckTime = 0f;

	private float activeCheckTime = 0f;

	[KSPField]
	public bool detectReverseThrust = false;

	[KSPField]
	public bool smoothRev = true;

	[KSPField]
	public float revDelay = 1f;

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

	private bool wasActive = false;

	private bool wasInactive = true;

	private double lastEnergy = 0.0;

	private double logdelay = 0.0;

	public Animation aniEngine = null;

	// was "= new ModuleEngines()": constructing a MonoBehaviour with new logs a Unity warning for every
	// part instance (prefab compile, editor, flight) and only produces a Unity-"null" object.
	private ModuleEngines myEngine = null;

	private Transform mytransform;

	private ModuleResourceIntake myIntake = null;

	private float lastThrottle = 0f;

	private Quaternion thrustRotation = new Quaternion(0f, 0f, 0f, 0f);

	private float lastdirection = 1f;

	private bool loadComplete = false;

	private Animation playAnimator;

	private string playAnimatorName;

	// Play_Anim runs every physics frame; part.FindModelAnimators allocates new arrays on every call.
	// The model's animators do not change, so look the first one up once per animation name.
	private Animation GetPlayAnimator(string aname)
	{
		if (playAnimatorName != aname || (Object)(object)playAnimator == (Object)null)
		{
			Animation[] array = ((PartModule)this).part.FindModelAnimators(aname);
			playAnimator = ((array.Length != 0) ? array[0] : null);
			playAnimatorName = aname;
		}
		return playAnimator;
	}

	public void Play_Anim(string aname, float aspeed, float atime = 0f)
	{
		logdelay -= Time.deltaTime;
		if (useRotorDiscSwap && (Object)(object)propeller != (Object)null && (Object)(object)rotorDisc != (Object)null)
		{
			if (Math.Abs(aspeed) >= rotorDiscFadeInStart)
			{
				((Component)rotorDisc).gameObject.GetComponent<Renderer>().enabled = true;
				((Component)propeller).gameObject.GetComponent<Renderer>().enabled = false;
			}
			else
			{
				((Component)rotorDisc).gameObject.GetComponent<Renderer>().enabled = false;
				((Component)propeller).gameObject.GetComponent<Renderer>().enabled = true;
			}
		}
		try
		{
			Animation val = GetPlayAnimator(aname);
			if ((Object)(object)val != (Object)null)
			{
				val.clip = val.GetClip(animationName);
				if (loopAnim)
				{
					val[aname].speed = aspeed * animSpeed;
					val[aname].wrapMode = (WrapMode)2;
				}
				else
				{
					val[aname].speed = aspeed;
					val[aname].normalizedTime = atime;
				}
				if (logdelay <= 0.0)
				{
					logdelay = 3.0;
				}
				val.Play(aname);
			}
		}
		catch (Exception ex)
		{
			Debug.Log((object)"[Maritime Pack] (aniEngine) Exception in Play_Anim");
			Debug.Log((object)("[Maritime Pack] (aniEngine) Err: " + ex));
		}
	}

	public void FAnimStartup()
	{
		if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
		{
			return;
		}
		if (partType == "Engine")
		{
			myEngine = ((IEnumerable)((PartModule)this).part.Modules).OfType<ModuleEngines>().FirstOrDefault();
			// guard: a part declaring partType = Engine without an engine module (or with a renamed thrust
			// transform) threw a NullReferenceException here in OnStartFinished, in the editor and in flight
			if ((Object)(object)myEngine != (Object)null)
			{
				mytransform = ((PartModule)this).part.FindModelTransform(myEngine.thrustVectorTransformName);
			}
			else
			{
				Debug.Log((object)("[Maritime Pack] (aniEngine) partType = Engine but no engine module on " + ((Object)((PartModule)this).part).name));
			}
			if ((Object)(object)mytransform != (Object)null)
			{
				thrustRotation = mytransform.localRotation;
			}
			if (useRotorDiscSwap)
			{
				Debug.Log((object)"[Maritime Pack] (aniEngine) Disc swapping active.");
				propeller = ((PartModule)this).part.FindModelTransform(propellerName);
				rotorDisc = ((PartModule)this).part.FindModelTransform(rotorDiscName);
				if ((Object)(object)propeller == (Object)null)
				{
					useRotorDiscSwap = false;
					Debug.Log((object)"[Maritime Pack] (aniEngine) Rotor Disc Swap enalbled but propeller gameObject not found");
				}
				if ((Object)(object)rotorDisc == (Object)null)
				{
					useRotorDiscSwap = false;
					Debug.Log((object)"[Maritime Pack] (aniEngine) Rotor Disc Swap enalbled but rotorDisc gameObject not found");
				}
				if ((Object)(object)rotorDisc != (Object)null)
				{
					try
					{
						((Component)rotorDisc).gameObject.GetComponent<Renderer>().enabled = false;
					}
					catch (Exception)
					{
						Debug.Log((object)"[Maritime Pack] (aniEngine) Mesh Renederer not found on rotorDisc!!!");
					}
				}
				if ((Object)(object)propeller != (Object)null)
				{
					try
					{
						((Component)propeller).gameObject.GetComponent<Renderer>().enabled = true;
					}
					catch (Exception)
					{
						Debug.Log((object)"[Maritime Pack] (aniEngine) Mesh Renederer not found on propeller!!!");
					}
				}
			}
		}
		if (HighLogic.LoadedSceneIsFlight)
		{
			Animation[] array = ((PartModule)this).part.FindModelAnimators(animationName);
			foreach (Animation val in array)
			{
				aniEngine = val;
				if (aniEngine.GetClipCount() > 1)
				{
					aniEngine.Stop();
				}
				Debug.Log((object)("[Maritime Pack] (aniEngine) Found animation: " + animationName + " on " + ((Object)((PartModule)this).part).name));
			}
		}
		if (partType == "Intake")
		{
			myIntake = ((IEnumerable)((PartModule)this).part.Modules).OfType<ModuleResourceIntake>().FirstOrDefault();
		}
	}

	public override void OnStartFinished(StartState state)
	{
		// was: ((PartModule)this).OnStartFinished(state), a virtual call back into this override (endless
		// recursion -> StackOverflowException logged for every part, FAnimStartup never ran).
		base.OnStartFinished(state);
		FAnimStartup();
		loadComplete = true;
	}

	private float CheckDirection()
	{
		float num = 1f;
		if ((Object)(object)mytransform == (Object)null)
		{
			return lastdirection;
		}
		Quaternion localRotation = mytransform.localRotation;
		if (!(thrustRotation == localRotation))
		{
			num = -1f;
		}
		if (num != lastdirection)
		{
			if (smoothRev)
			{
				if (num == -1f)
				{
					lastdirection -= revDelay;
				}
				else
				{
					lastdirection += revDelay;
				}
				if (lastdirection > 1f)
				{
					lastdirection = 1f;
				}
				if (lastdirection < -1f)
				{
					lastdirection = -1f;
				}
			}
			else
			{
				lastdirection = num;
			}
		}
		return lastdirection;
	}

	public void FixedUpdate()
	{
		if (!HighLogic.LoadedSceneIsFlight || !loadComplete || (Object)(object)((PartModule)this).vessel == (Object)null || ((Object)(object)myEngine == (Object)null && (Object)(object)myIntake == (Object)null && partType != "Throttle" && partType != "Stirling" && partType != "Battery") || animationName == "null" || !((PartModule)this).vessel.isActiveVessel)
		{
			return;
		}
		// was: NullReferenceException every physics frame when the engine/intake module or the animation
		// named in the cfg is missing
		if ((partType == "Engine" && (Object)(object)myEngine == (Object)null) || (partType == "Intake" && (Object)(object)myIntake == (Object)null) || (Object)(object)aniEngine == (Object)null)
		{
			return;
		}
		double num = 0.0;
		if (partType == "Stirling")
		{
			num = Math.Abs(((PartModule)this).part.thermalRadiationFlux) / 100.0;
			if (num > 1.0)
			{
				num = 1.0;
			}
			if (num < 0.0)
			{
				num = 0.0;
			}
			if (animSpeed == 0f)
			{
				Play_Anim(animationName, 0f, (float)num);
			}
			else
			{
				Play_Anim(animationName, (float)num);
			}
			return;
		}
		if (!(partType == "Battery"))
		{
			num = ((!syncThrottle) ? (((Object)(object)myEngine != (Object)null) ? ((double)myEngine.normalizedThrustOutput) : 0.0) : ((double)FlightInputHandler.state.mainThrottle));
		}
		else
		{
			double resourceTotal = FengistAnimFunctions.GetResourceTotal(FlightGlobals.ActiveVessel, "ElectricCharge");
			double resourceMax = FengistAnimFunctions.GetResourceMax(FlightGlobals.ActiveVessel, "ElectricCharge");
			num = ((resourceMax > 0.0) ? (resourceTotal / resourceMax) : 0.0); // NaN on vessels without EC storage
		}
		float num2 = 1f;
		if (detectReverseThrust)
		{
			num2 = CheckDirection();
		}
		if ((partType == "Engine" && !myEngine.EngineIgnited) || (partType == "Engine" && myEngine.getFlameoutState) || (partType == "Intake" && !myIntake.intakeEnabled))
		{
			if (syncThrust)
			{
				wasInactive = true;
				activeCheckTime = 0f;
				if (wasActive && inactiveCheckTime > 0f && spoolTime > 0f)
				{
					if (inactiveCheckTime <= 0f)
					{
						wasActive = false;
					}
					else
					{
						inactiveCheckTime -= Time.deltaTime;
						num = (double)(inactiveCheckTime / spoolTime) * lastEnergy;
					}
				}
				Play_Anim(animationName, (float)num * num2);
			}
			else
			{
				if (lastThrottle == 0f)
				{
					return;
				}
				if (syncThrottle)
				{
					if (smoothThrottle)
					{
						lastThrottle -= 0.02f;
						if (lastThrottle < 0f)
						{
							lastThrottle = 0f;
						}
					}
					else
					{
						lastThrottle = 0f;
					}
					if (loopAnim)
					{
						Play_Anim(animationName, lastThrottle);
					}
					else
					{
						Play_Anim(animationName, 0f, lastThrottle);
					}
				}
				else if (!aniEngine.isPlaying)
				{
					if (loopAnim)
					{
						Play_Anim(animationName, 0f);
					}
					else
					{
						Play_Anim(animationName, -1f, 1f);
					}
					lastThrottle = 0f;
				}
			}
		}
		else if (syncThrust)
		{
			if (num < (double)minSpeed)
			{
				num = minSpeed;
			}
			wasActive = true;
			inactiveCheckTime = spoolTime;
			if (wasInactive && activeCheckTime < spoolTime && spoolTime > 0f)
			{
				if (activeCheckTime > spoolTime)
				{
					wasInactive = false;
				}
				else
				{
					activeCheckTime += Time.deltaTime;
					num = (double)(activeCheckTime / spoolTime) * num;
				}
			}
			if (loopAnim)
			{
				Play_Anim(animationName, (float)num * num2);
			}
			else
			{
				Play_Anim(animationName, 0f, (float)num);
			}
			lastEnergy = num;
		}
		else if (syncThrottle)
		{
			if (smoothThrottle)
			{
				if (num > (double)lastThrottle)
				{
					if (num + (double)lastThrottle > 0.019999999552965164)
					{
						lastThrottle += 0.02f;
						if (lastThrottle > 1f)
						{
							lastThrottle = 1f;
						}
					}
					else
					{
						lastThrottle = (float)num;
					}
				}
				if (num < (double)lastThrottle)
				{
					if ((double)lastThrottle - num > 0.019999999552965164 || (num == 0.0 && lastThrottle != 0f))
					{
						lastThrottle -= 0.02f;
						if (lastThrottle < 0f)
						{
							lastThrottle = 0f;
						}
					}
					else
					{
						lastThrottle = (float)num;
					}
				}
				if (loopAnim)
				{
					Play_Anim(animationName, lastThrottle);
				}
				else
				{
					Play_Anim(animationName, 0f, lastThrottle);
				}
			}
			else if (loopAnim)
			{
				Play_Anim(animationName, (float)num);
			}
			else
			{
				Play_Anim(animationName, 0f, (float)num);
			}
		}
		else if (!aniEngine.isPlaying && lastThrottle != 1f)
		{
			Play_Anim(animationName, 1f);
			lastThrottle = 1f;
		}
	}

	public FAnimEngine()
	{
	}
}
