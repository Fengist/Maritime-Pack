namespace FengistAnim;

public class ModuleLayeredAnimator : ModuleAnimateGeneric
{
	[KSPField]
	public int layer = 1;

	public override void OnStart(StartState state)
	{
		base.anim[base.animationName].layer = layer;
	}
}
