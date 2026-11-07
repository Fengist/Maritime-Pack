namespace FengistAnim;

public static class FengistAnimFunctions
{
	public static double GetResourceAmount(this Part part, string resourceName)
	{
		PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(resourceName);
		return part.Resources.Get(definition.id).amount;
	}

	public static double GetResourceTotal(Vessel v, string resourceName)
	{
		PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(resourceName);
		double num = 0.0;
		foreach (Part part in v.parts)
		{
			if (part.Resources.Contains(resourceName))
			{
				num += part.GetResourceAmount(resourceName);
			}
		}
		return num;
	}

	public static double GetResourceMax(Vessel v, string resourceName)
	{
		PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(resourceName);
		double num = 0.0;
		foreach (Part part in v.parts)
		{
			if (part.Resources.Contains(resourceName))
			{
				num += part.Resources.Get(definition.id).maxAmount;
			}
		}
		return num;
	}
}
