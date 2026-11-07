using System;
using UnityEngine;
using Random = System.Random;

namespace MaritimePack.engine;

public class MPengineSounds : PartModule
{
	[KSPField]
	public string engage;

	[KSPField]
	public string running;

	[KSPField]
	public string power;

	[KSPField]
	public string disengage;

	[KSPField]
	public string flameout;

	[KSPField]
	public string warning;

	[KSPField]
	public float powerFadeInSpeed = 0.02f;

	[KSPField]
	public float powerFadeInDelay = 0f;

	[KSPField]
	public float powerLowerThreshold = 0f;

	[KSPField]
	public float runningVolume = 1f;

	[KSPField]
	public float powerVolume = 1f;

	[KSPField]
	public float engageVolume = 1f;

	[KSPField]
	public float disengageVolume = 1f;

	[KSPField]
	public float flameoutVolume = 1f;

	[KSPField]
	public float warningVolume = 1f;

	[KSPField]
	public float randomStartDelay = 0f;

	[KSPField]
	public float warningSoundThreshold = 0.8f;

	[KSPField]
	public float warningCooldownTime = 2f;

	private float warningCountDown = 0f;

	[KSPField]
	public float powerPitchBase = 0.8f;

	[KSPField]
	public float thrustAddedToPitch = 0.3f;

	[KSPField]
	public bool useDebug = false;

	[KSPField]
	public bool showVolumeDebug = false;

	[KSPField]
	public bool singleMethod = false;

	private MPengineWrapper engine;

	private bool oldIgnitionState;

	private bool oldFlameOutState;

	private float currentPowerFadeIn;

	private float currentPowerDelay;

	private float smoothedPowerPitch = 0f;

	private float maxThrust;

	private bool paused = false;

	private Random rand = new Random();

	private string doesExist = "...";

	public FXGroup engageGroup;

	public bool engageAssigned;

	public FXGroup runningGroup;

	public bool runningAssigned;

	public FXGroup powerGroup;

	public bool powerAssigned;

	public FXGroup disengageGroup;

	public bool disengageAssigned;

	public FXGroup flameoutGroup;

	public bool flameoutAssigned;

	public FXGroup warningGroup;

	public bool warningAssigned;

	public bool createGroup(FXGroup group, string name, bool loop)
	{
		// unset sound fields are null, not string.Empty; a null group would throw below
		if (group != null && !string.IsNullOrEmpty(name))
		{
			if (!GameDatabase.Instance.ExistsAudioClip(name))
			{
				return false;
			}
			group.audio = ((Component)this).gameObject.AddComponent<AudioSource>();
			group.audio.playOnAwake = false;
			group.audio.loop = loop;
			group.audio.rolloffMode = (AudioRolloffMode)0;
			group.audio.dopplerLevel = 0f;
			group.audio.volume = GameSettings.SHIP_VOLUME;
			group.audio.spatialBlend = 1f;
			group.audio.clip = GameDatabase.Instance.GetAudioClip(name);
			return true;
		}
		return false;
	}

	public void OnPause()
	{
		if (engageAssigned)
		{
			engageGroup.audio.volume = 0f;
		}
		if (runningAssigned)
		{
			runningGroup.audio.volume = 0f;
		}
		if (powerAssigned)
		{
			powerGroup.audio.volume = 0f;
		}
		if (disengageAssigned)
		{
			disengageGroup.audio.volume = 0f;
		}
		if (flameoutAssigned)
		{
			flameoutGroup.audio.volume = 0f;
		}
		if (warningAssigned)
		{
			warningGroup.audio.volume = 0f;
		}
		paused = true;
	}

	public void OnResume()
	{
		if (engageAssigned)
		{
			engageGroup.audio.volume = GameSettings.SHIP_VOLUME * engageVolume;
		}
		if (runningAssigned)
		{
			runningGroup.audio.volume = GameSettings.SHIP_VOLUME * runningVolume;
		}
		if (powerAssigned)
		{
			powerGroup.audio.volume = GameSettings.SHIP_VOLUME * powerVolume;
		}
		if (disengageAssigned)
		{
			disengageGroup.audio.volume = GameSettings.SHIP_VOLUME * disengageVolume;
		}
		if (flameoutAssigned)
		{
			flameoutGroup.audio.volume = GameSettings.SHIP_VOLUME * flameoutVolume;
		}
		if (warningAssigned)
		{
			warningGroup.audio.volume = GameSettings.SHIP_VOLUME * warningVolume;
		}
		paused = false;
	}

	public override void OnStart(StartState state)
	{
		((PartModule)this).OnStart(state);
		// FXGroup fields are normally created by Unity serialization when the part is instantiated;
		// make sure they exist so createGroup can never be handed a null group.
		if (engageGroup == null)
		{
			engageGroup = new FXGroup("engage");
		}
		if (runningGroup == null)
		{
			runningGroup = new FXGroup("running");
		}
		if (powerGroup == null)
		{
			powerGroup = new FXGroup("power");
		}
		if (disengageGroup == null)
		{
			disengageGroup = new FXGroup("disengage");
		}
		if (flameoutGroup == null)
		{
			flameoutGroup = new FXGroup("flameout");
		}
		if (warningGroup == null)
		{
			warningGroup = new FXGroup("warning");
		}
		engageAssigned = createGroup(engageGroup, engage, loop: false);
		runningAssigned = createGroup(runningGroup, running, loop: true);
		powerAssigned = createGroup(powerGroup, power, loop: true);
		disengageAssigned = createGroup(disengageGroup, disengage, loop: false);
		flameoutAssigned = createGroup(flameoutGroup, flameout, loop: false);
		warningAssigned = createGroup(warningGroup, warning, loop: false);
		engine = new MPengineWrapper(((PartModule)this).part);
		if (engine != null)
		{
			maxThrust = engine.maxThrust;
		}
	}

	private void Update()
	{
		if (FlightDriver.Pause != paused)
		{
			if (FlightDriver.Pause)
			{
				OnPause();
			}
			else
			{
				OnResume();
			}
		}
	}

	public void FixedUpdate()
	{
		((PartModule)this).OnFixedUpdate();
		if (!HighLogic.LoadedSceneIsFlight || engine == null || (UnityEngine.Object)(object)((PartModule)this).part == (UnityEngine.Object)null)
		{
			return;
		}
		if (engine.type == MPengineWrapper.EngineType.FSengine)
		{
			maxThrust = engine.maxThrust;
		}
		if ((int)TimeWarp.WarpMode == 0 && TimeWarp.CurrentRate > 1.1f)
		{
			if (powerAssigned)
			{
				powerGroup.audio.volume = 0f;
			}
		}
		else
		{
			if (singleMethod && powerAssigned && engageAssigned && disengageAssigned)
			{
				if (engine.getIgnitionState && !oldIgnitionState)
				{
					if (!engine.getFlameoutState)
					{
						engageGroup.audio.volume = GameSettings.SHIP_VOLUME * engageVolume;
						engageGroup.audio.Play();
						if (disengageAssigned && disengageGroup.audio.isPlaying)
						{
							disengageGroup.audio.Stop();
						}
						if (powerAssigned && powerGroup.audio.isPlaying)
						{
							powerGroup.audio.Stop();
						}
						if (flameoutAssigned && flameoutGroup.audio.isPlaying)
						{
							flameoutGroup.audio.Stop();
						}
					}
				}
				else if (!engine.getIgnitionState && oldIgnitionState)
				{
					if (!engine.getFlameoutState)
					{
						disengageGroup.audio.volume = GameSettings.SHIP_VOLUME * disengageVolume;
						disengageGroup.audio.Play();
						if (flameoutAssigned && flameoutGroup.audio.isPlaying)
						{
							flameoutGroup.audio.Stop();
						}
						if (engageAssigned && engageGroup.audio.isPlaying)
						{
							engageGroup.audio.Stop();
						}
						if (powerAssigned && powerGroup.audio.isPlaying)
						{
							powerGroup.audio.Stop();
						}
					}
				}
				else if (engine.getIgnitionState && !engine.getFlameoutState && !engageGroup.audio.isPlaying)
				{
					float num = ThrustFraction() * thrustAddedToPitch + powerPitchBase;
					smoothedPowerPitch = Mathf.Lerp(smoothedPowerPitch, num, 0.1f);
					if (!powerGroup.audio.isPlaying)
					{
						powerGroup.audio.Play();
					}
					powerGroup.audio.pitch = smoothedPowerPitch;
				}
			}
			else
			{
				if (engine.getIgnitionState && !engine.getFlameoutState)
				{
					if (powerAssigned)
					{
						if (currentPowerDelay > 0f)
						{
							currentPowerDelay -= Time.deltaTime;
						}
						float num2 = ThrustFraction() * thrustAddedToPitch + powerPitchBase;
						smoothedPowerPitch = Mathf.Lerp(smoothedPowerPitch, num2, 0.1f);
						if (currentPowerDelay <= 0f)
						{
							if (currentPowerFadeIn < 1f)
							{
								currentPowerFadeIn += powerFadeInSpeed;
								if (currentPowerFadeIn > 1f)
								{
									currentPowerFadeIn = 1f;
								}
							}
							if (ThrustFraction() > powerLowerThreshold || powerLowerThreshold <= 0f)
							{
								powerGroup.audio.volume = GameSettings.SHIP_VOLUME * currentPowerFadeIn * powerVolume;
							}
							else
							{
								powerGroup.audio.volume = Mathf.Lerp(powerGroup.audio.volume, 0f, 0.06f);
							}
							if (!powerGroup.audio.isPlaying)
							{
								powerGroup.audio.Play();
							}
							powerGroup.audio.pitch = smoothedPowerPitch;
						}
					}
					if (runningAssigned && !runningGroup.audio.isPlaying)
					{
						runningGroup.audio.volume = GameSettings.SHIP_VOLUME * runningVolume;
						runningGroup.audio.Play();
					}
				}
				else
				{
					if (powerAssigned)
					{
						if (powerGroup.audio.isPlaying)
						{
							powerGroup.audio.Stop();
						}
						if (!engine.getIgnitionState)
						{
							currentPowerFadeIn = 0f;
							currentPowerDelay = powerFadeInDelay + (float)rand.NextDouble() * randomStartDelay;
						}
					}
					if (runningAssigned && runningGroup.audio.isPlaying)
					{
						runningGroup.audio.Stop();
					}
				}
				if (engageAssigned && engine.getIgnitionState && !oldIgnitionState && !engine.getFlameoutState)
				{
					engageGroup.audio.volume = GameSettings.SHIP_VOLUME * engageVolume;
					engageGroup.audio.Play();
					if (disengageAssigned && disengageGroup.audio.isPlaying)
					{
						disengageGroup.audio.Stop();
					}
					if (flameoutAssigned && flameoutGroup.audio.isPlaying)
					{
						flameoutGroup.audio.Stop();
					}
				}
				if (disengageAssigned && !engine.getIgnitionState && oldIgnitionState && !engine.getFlameoutState)
				{
					disengageGroup.audio.volume = GameSettings.SHIP_VOLUME * disengageVolume;
					disengageGroup.audio.Play();
					if (engageAssigned && engageGroup.audio.isPlaying)
					{
						engageGroup.audio.Stop();
					}
				}
				if (flameoutAssigned && engine.getFlameoutState && !oldFlameOutState)
				{
					flameoutGroup.audio.volume = GameSettings.SHIP_VOLUME * flameoutVolume;
					flameoutGroup.audio.Play();
					if (engageAssigned && engageGroup.audio.isPlaying)
					{
						engageGroup.audio.Stop();
					}
				}
			}
			oldIgnitionState = engine.getIgnitionState;
			oldFlameOutState = engine.getFlameoutState;
		}
		if (warningAssigned)
		{
			if (warningCountDown > 0f)
			{
				warningCountDown -= Time.deltaTime;
			}
			if (((PartModule)this).part.temperature > ((PartModule)this).part.maxTemp * (double)warningSoundThreshold && warningCountDown <= 0f)
			{
				warningGroup.audio.volume = GameSettings.SHIP_VOLUME * warningVolume;
				warningGroup.audio.Play();
				warningCountDown = warningCooldownTime;
			}
		}
	}

	// finalThrust / maxThrust, but 0 instead of NaN when maxThrust is 0 (NaN pitch makes Unity log errors)
	private float ThrustFraction()
	{
		if (engine == null || !(maxThrust > 0f))
		{
			return 0f;
		}
		return engine.finalThrust / maxThrust;
	}

	public void OnGUI()
	{
		if (showVolumeDebug)
		{
			GUI.Label(new Rect(250f, 300f, 200f, 30f), "engine volume: " + currentPowerFadeIn);
		}
		if (!useDebug || !HighLogic.LoadedSceneIsEditor)
		{
			return;
		}
		Rect val = new Rect(250f, 200f, 300f, 150f);
		Vector2 val2 = new Vector2(30f, 30f);
		Vector2 val3 = new Vector2(200f, 35f);
		if (GUI.Button(new Rect(val.x, val.y, val2.x, val2.y), "OK"))
		{
			if (GameDatabase.Instance.ExistsAudioClip(warning))
			{
				doesExist = "Yes";
				createGroup(warningGroup, warning, loop: false);
			}
			else
			{
				doesExist = "No";
			}
		}
		if (GUI.Button(new Rect(val.x + val2.x + 10f, val.y, val2.x, val2.y), "C"))
		{
			doesExist = "clear";
		}
		if (GUI.Button(new Rect(val.x + (val2.x + 10f) * 2f, val.y, val2.x, val2.y), ">") && warningAssigned)
		{
			warningGroup.audio.Play();
		}
		GUI.Label(new Rect(val.x + (val2.x + 10f) * 3f, val.y, val3.x - val2.x - 10f, val2.y), "exists: " + doesExist);
		val.y += 30f;
		warning = GUI.TextField(new Rect(val.x + val2.x + 10f, val.y, val3.x - val2.x - 10f, val2.y), warning);
	}
}
