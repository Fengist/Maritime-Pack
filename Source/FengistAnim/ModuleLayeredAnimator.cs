namespace FengistAnim;

public class ModuleLayeredAnimator : ModuleAnimateGeneric
{
	// KSP 1.12's ModuleAnimateGeneric already declares [KSPField] int layer and applies it in OnStart.
	// Re-declaring it hid the base field, and skipping base.OnStart left the module uninitialised
	// (anim could still be null here -> NullReferenceException, and Toggle never worked).
	public override void OnStart(StartState state)
	{
		base.OnStart(state);
		if ((UnityEngine.Object)(object)base.anim != (UnityEngine.Object)null && !string.IsNullOrEmpty(base.animationName) && base.anim[base.animationName] != null)
		{
			base.anim[base.animationName].layer = base.layer;
		}
	}
}
