using System.Collections;
using System.Linq;
using UnityEngine;

namespace MaritimePack.engine;

internal class MPengineWrapper
{
	public enum EngineType
	{
		NONE,
		ModuleEngine,
		ModuleEngineFX,
		FSengine
	}

	public EngineType type = EngineType.NONE;

	public ModuleEngines engine;

	public ModuleEnginesFX engineFX;

	public float maxThrust
	{
		get
		{
			return type switch
			{
				EngineType.ModuleEngine => engine.maxThrust, 
				EngineType.ModuleEngineFX => ((ModuleEngines)engineFX).maxThrust, 
				_ => 0f, 
			};
		}
		set
		{
			switch (type)
			{
			case EngineType.ModuleEngine:
				engine.maxThrust = value;
				break;
			case EngineType.ModuleEngineFX:
				((ModuleEngines)engineFX).maxThrust = value;
				break;
			}
		}
	}

	public float minThrust
	{
		get
		{
			return type switch
			{
				EngineType.ModuleEngine => engine.minThrust, 
				EngineType.ModuleEngineFX => ((ModuleEngines)engineFX).minThrust, 
				_ => 0f, 
			};
		}
		set
		{
			switch (type)
			{
			case EngineType.ModuleEngine:
				engine.minThrust = value;
				break;
			case EngineType.ModuleEngineFX:
				((ModuleEngines)engineFX).minThrust = value;
				break;
			}
		}
	}

	public bool EngineIgnited
	{
		get
		{
			return type switch
			{
				EngineType.ModuleEngine => engine.EngineIgnited, 
				EngineType.ModuleEngineFX => ((ModuleEngines)engineFX).EngineIgnited, 
				_ => false, 
			};
		}
		set
		{
			switch (type)
			{
			case EngineType.ModuleEngine:
				engine.EngineIgnited = value;
				break;
			case EngineType.ModuleEngineFX:
				((ModuleEngines)engineFX).EngineIgnited = value;
				break;
			}
		}
	}

	public bool flameout
	{
		get
		{
			return type switch
			{
				EngineType.ModuleEngine => engine.flameout, 
				EngineType.ModuleEngineFX => ((ModuleEngines)engineFX).flameout, 
				_ => false, 
			};
		}
		set
		{
			switch (type)
			{
			case EngineType.ModuleEngine:
				engine.flameout = value;
				break;
			case EngineType.ModuleEngineFX:
				((ModuleEngines)engineFX).flameout = value;
				break;
			}
		}
	}

	public bool getIgnitionState => type switch
	{
		EngineType.ModuleEngine => engine.getIgnitionState, 
		EngineType.ModuleEngineFX => ((ModuleEngines)engineFX).getIgnitionState, 
		_ => false, 
	};

	public bool getFlameoutState => type switch
	{
		EngineType.ModuleEngine => engine.getFlameoutState, 
		EngineType.ModuleEngineFX => ((ModuleEngines)engineFX).getFlameoutState, 
		_ => false, 
	};

	public float engineAccelerationSpeed
	{
		get
		{
			return type switch
			{
				EngineType.ModuleEngine => engine.engineAccelerationSpeed, 
				EngineType.ModuleEngineFX => ((ModuleEngines)engineFX).engineAccelerationSpeed, 
				_ => 0f, 
			};
		}
		set
		{
			switch (type)
			{
			case EngineType.ModuleEngine:
				engine.engineAccelerationSpeed = value;
				break;
			case EngineType.ModuleEngineFX:
				((ModuleEngines)engineFX).engineAccelerationSpeed = value;
				break;
			}
		}
	}

	public float engineDecelerationSpeed
	{
		get
		{
			return type switch
			{
				EngineType.ModuleEngine => engine.engineDecelerationSpeed, 
				EngineType.ModuleEngineFX => ((ModuleEngines)engineFX).engineDecelerationSpeed, 
				_ => 0f, 
			};
		}
		set
		{
			switch (type)
			{
			case EngineType.ModuleEngine:
				engine.engineDecelerationSpeed = value;
				break;
			case EngineType.ModuleEngineFX:
				((ModuleEngines)engineFX).engineDecelerationSpeed = value;
				break;
			}
		}
	}

	public float finalThrust
	{
		get
		{
			return type switch
			{
				EngineType.ModuleEngine => engine.finalThrust, 
				EngineType.ModuleEngineFX => ((ModuleEngines)engineFX).finalThrust, 
				_ => 0f, 
			};
		}
		set
		{
			switch (type)
			{
			case EngineType.ModuleEngine:
				engine.finalThrust = value;
				break;
			case EngineType.ModuleEngineFX:
				((ModuleEngines)engineFX).finalThrust = value;
				break;
			}
		}
	}

	public float finalThrustNormalized => (maxThrust > 0f) ? (finalThrust / maxThrust) : 0f;

	public bool throttleLocked
	{
		get
		{
			return type switch
			{
				EngineType.ModuleEngine => engine.throttleLocked, 
				EngineType.ModuleEngineFX => ((ModuleEngines)engineFX).throttleLocked, 
				_ => false, 
			};
		}
		set
		{
			switch (type)
			{
			case EngineType.ModuleEngine:
				engine.throttleLocked = value;
				break;
			case EngineType.ModuleEngineFX:
				((ModuleEngines)engineFX).throttleLocked = value;
				break;
			}
		}
	}

	public MPengineWrapper(Part part)
	{
		engine = ((IEnumerable)part.Modules).OfType<ModuleEngines>().FirstOrDefault();
		if ((Object)(object)engine != (Object)null)
		{
			type = EngineType.ModuleEngine;
			return;
		}
		engineFX = ((IEnumerable)part.Modules).OfType<ModuleEnginesFX>().FirstOrDefault();
		// was: type = ModuleEngineFX unconditionally. On a part without any engine module every property
		// getter then dereferenced a null engineFX (NullReferenceException in OnStart and every FixedUpdate).
		type = (((Object)(object)engineFX != (Object)null) ? EngineType.ModuleEngineFX : EngineType.NONE);
	}

	public MPengineWrapper(Part part, string name)
	{
		engineFX = (from p in ((IEnumerable)part.Modules).OfType<ModuleEnginesFX>()
			where ((ModuleEngines)p).engineID == name
			select p).FirstOrDefault();
		if ((Object)(object)engineFX != (Object)null)
		{
			type = EngineType.ModuleEngineFX;
		}
	}
}
