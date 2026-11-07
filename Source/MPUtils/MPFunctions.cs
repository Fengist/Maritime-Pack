/*********************************************************************************************
* The Maritime Pack MPUtil plugin is copyright 2015 Fengist, all rights reserved.
* For full license information please visit http://www.kerbaltopia.com
*
* Version detection code originally by Ialdabaoth
*********************************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using KSP.IO;
using System.IO;

namespace MPUtils
{

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class MPStartup : MonoBehaviour
    {
        public static bool MPUtilsrunning;
        private bool multipleCopies = false;

        public void Awake()
        {
            MPConfig.Getconfig();
            MPLog.NewLog();
            Application.runInBackground = true;
            if (MPUtilsrunning || !ElectionAndCheck())
            {
                MPLog.Writelog("[Maritime Pack] Multiple copies. Using the first copy. Version: " + MPFunctions.GetVersion());
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
            MPUtilsrunning = true;
            MPLog.Writelog("[Maritime Pack] MPUtils v" + MPFunctions.GetVersion() + " Anchors Aweigh!");
        }

        public void Start()
        {
            if (multipleCopies == true)
            {
                //PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Multiple Copies of MPUtils", "[ERROR] Multiple copies of MPUTils were found! This may lead to unpredicable results.", "Ok", false, HighLogic.UISkin);
            }
        }

        public bool ElectionAndCheck()
        {
            #region Type election

            // TODO : Move the old version check in a process that call Update.

            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            IEnumerable<AssemblyLoader.LoadedAssembly> eligible = from a in AssemblyLoader.loadedAssemblies
                                                                  let ass = a.assembly
                                                                  where ass.GetName().Name == currentAssembly.GetName().Name
                                                                  orderby ass.GetName().Version descending, a.path ascending
                                                                  select a;

            if (eligible.Count()>1)
            {
                multipleCopies = true;
            }
            if (eligible.First().assembly != currentAssembly)
            {
                //loaded = true;
                MPLog.Writelog("version " + currentAssembly.GetName().Version + " at " + currentAssembly.Location +
                    " lost the election");
                Destroy(gameObject);
                return false;
            }
            string candidates = "";
            foreach (AssemblyLoader.LoadedAssembly a in eligible)
            {
                if (currentAssembly.Location != a.path)
                    candidates += "Version " + a.assembly.GetName().Version + " " + a.path + " " + "\n";
            }
            if (candidates.Length > 0)
            {
                MPLog.Writelog("version " + currentAssembly.GetName().Version + " at " + currentAssembly.Location +
                    " won the election against\n" + candidates);
            }

            #endregion Type election

            return true;
        }


        public void FixedUpdate()
        {
            if (!MPUtilsrunning) { return; }
        }
    }

    class MPLog
    {
        private const string MPPath = "GameData/MPUtils/Plugins/PluginData/";
        private const string MPDebugLog = "MPLog.dat";

        public static void NewLog()
        {
            string path = KSPUtil.ApplicationRootPath + MPPath;
            string filepath = System.IO.Path.Combine(path, MPDebugLog);
            System.IO.File.Delete(filepath);
        }

        public static void Writelog(string thisline)
        {
            string path = KSPUtil.ApplicationRootPath + MPPath;
            string filepath = System.IO.Path.Combine(path, MPDebugLog);
            if (MPConfig.MPWLog)
            {
                System.IO.File.AppendAllText(filepath, DateTime.Now.ToString() + ": " + thisline + Environment.NewLine);
            }
            if (MPConfig.KSPWLog)
            {
                Debug.Log(thisline);
            }
        }
    }

    class MPConfig
    {
        public static bool MPWLog;
        public static bool KSPWLog;
        public static float MPMaxDepth;
        public static bool MPDepthWarn;
        public static bool MPLoadIcons;

        public static void Getconfig()
        {
            Debug.Log("[Maritime Pack] : cfg file loading...");
            PluginConfiguration cfg = PluginConfiguration.CreateForType<MPConfig>();
            cfg.load();
            MPWLog = cfg.GetValue<bool>("MPWLog", false);
            KSPWLog = cfg.GetValue<bool>("KSPWLog", false);
            MPDepthWarn = cfg.GetValue<bool>("MPDepthWarn", false);
            MPMaxDepth = cfg.GetValue<float>("MPMaxDepth", 990.00f);
            MPLoadIcons = cfg.GetValue<bool>("MPLoadIcons", true);
            Debug.Log("[Maritime Pack] : cfg file loaded.");
        }
    }

    public static class MPFunctions
    {

        private static FieldInfo windowListField;

        public static List<int> parseIntegers(string stringOfInts)
        {
            List<int> newIntList = new List<int>();
            string[] valueArray = stringOfInts.Split(';');
            for (int i = 0; i < valueArray.Length; i++)
            {
                int newValue = 0;
                if (int.TryParse(valueArray[i], out newValue))
                {
                    newIntList.Add(newValue);
                }
                else
                {
                    Debug.Log("invalid integer: " + valueArray[i]);
                }
            }
            return newIntList;
        }

        public static UIPartActionWindow FindActionWindow(this Part part)
        {
            if (part == null)
                return null;

            // We need to do quite a bit of piss-farting about with reflection to 
            // dig the thing out. We could just use Object.Find, but that requires hitting a heap more objects.
            UIPartActionController controller = UIPartActionController.Instance;
            if (controller == null)
                return null;

            if (windowListField == null)
            {
                Type cntrType = typeof(UIPartActionController);
                foreach (FieldInfo info in cntrType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (info.FieldType == typeof(List<UIPartActionWindow>))
                    {
                        windowListField = info;
                        goto foundField;
                    }
                }
                Debug.LogWarning("*PartUtils* Unable to find UIPartActionWindow list");
                return null;
            }
        foundField:

            List<UIPartActionWindow> uiPartActionWindows = (List<UIPartActionWindow>)windowListField.GetValue(controller);
            if (uiPartActionWindows == null)
                return null;

            return uiPartActionWindows.FirstOrDefault(window => window != null && window.part == part);
        }
        public static List<string> parseNames(string names)
        {
            return parseNames(names, false, true, string.Empty);
        }

        public static List<string> parseNames(string names, bool replaceBackslashErrors)
        {
            return parseNames(names, replaceBackslashErrors, true, string.Empty);
        }

        public static List<string> parseNames(string names, bool replaceBackslashErrors, bool trimWhiteSpace, string prefix)
        {
            List<string> source = names.Split(';').ToList<string>();
            for (int i = source.Count - 1; i >= 0; i--)
            {
                if (source[i] == string.Empty)
                {
                    source.RemoveAt(i);
                }
            }
            if (trimWhiteSpace)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    source[i] = source[i].Trim(' ');
                }
            }
            if (prefix != string.Empty)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    source[i] = prefix + source[i];
                }
            }
            if (replaceBackslashErrors)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    source[i] = source[i].Replace('\\', '/');
                }
            }
            return source.ToList<string>();
        }

        public static List<double> parseDoubles(string stringOfDoubles)
        {
            System.Collections.Generic.List<double> list = new System.Collections.Generic.List<double>();
            string[] array = stringOfDoubles.Trim().Split(';');
            for (int i = 0; i < array.Length; i++)
            {
                double item = 0f;
                if (double.TryParse(array[i].Trim(), out item))
                {
                    list.Add(item);
                }
                else
                {
                    Debug.Log("FStools: invalid float: [len:" + array[i].Length + "] '" + array[i] + "']");
                }
            }
            return list;
        }

        public static string GetVersion()
        {
    
            string version = Assembly.GetExecutingAssembly()
                                        .GetName()
                                        .Version
                                        .ToString();
            return version;
        }


        public static double GetDensity(double altitude, CelestialBody body)
        {
            if (!body.atmosphere)
                return 0;

            if (altitude > body.atmosphereDepth)
                return 0;

            double pressure = body.GetPressure(altitude);

            // get an average day/night temperature at the equator
            double sunDot = 0.5;
            float sunAxialDot = 0;
            double atmosphereTemperatureOffset = (double)body.latitudeTemperatureBiasCurve.Evaluate(0) + (double)body.latitudeTemperatureSunMultCurve.Evaluate(0) * sunDot + (double)body.axialTemperatureSunMultCurve.Evaluate(sunAxialDot);
            double temperature = body.GetTemperature(altitude) + (double)body.atmosphereTemperatureSunMultCurve.Evaluate((float)altitude) * atmosphereTemperatureOffset;

            return body.GetDensity(pressure, temperature);
        }

        public static Part GetResourcePart(Vessel v, string resourceName)
        {
            foreach (Part mypart in v.parts)
            {
                if (mypart.Resources.Contains(resourceName))
                {
                    return mypart;
                }
            }
            return null;
        }

        public static Vector3 GetSize(Vessel v)
        {
            Bounds bounds = default(Bounds);
            Vector3 orgPos = v.parts[0].orgPos;
            bounds.center = orgPos;
            List<Bounds> list = new List<Bounds>();
            foreach (Part current in v.parts)
            {
                MPLog.Writelog("[Maritime Pack] part: " + current.name + " WCoM" + current.WCoM);
                MPLog.Writelog("[Maritime Pack] part: " + current.name + " CoB" + current.CenterOfBuoyancy);
                MPLog.Writelog("[Maritime Pack] part: " + current.name + " CoD" + current.CenterOfDisplacement);

                Bounds[] partRendererBounds = PartGeometryUtil.GetPartRendererBounds(current);
                Bounds[] array = partRendererBounds;
                for (int i = 0; i < array.Length; i++)
                {
                    Bounds bounds2 = array[i];
                    Bounds bounds3 = bounds2;
                    bounds3.size *= current.boundsMultiplier;
                    Vector3 size = bounds3.size;
                    bounds3.Expand(current.GetModuleSize(size));
                    list.Add(bounds2);
                }
            }
            return PartGeometryUtil.MergeBounds(list.ToArray(), v.parts[0].transform.root).size;
        }

        public static float GetVolume(Vessel v)
        {
            float volume =0.0f;
            foreach (Part mypart in v.parts)
            {
                var boundsSize = PartGeometryUtil.MergeBounds(mypart.GetRendererBounds(), mypart.transform).size;
                volume = boundsSize.x * boundsSize.y * boundsSize.z * 1000f;
            }
            return volume;
        }

        public static double GetResourceTotal(Vessel v, string resourceName)
        {
            PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(resourceName);
            double amount = 0;
            foreach (Part mypart in v.parts)
            {
                if (mypart.Resources.Contains(resourceName))
                {
                    amount += MPFunctions.GetResourceAmount(mypart, resourceName);
                }
            }
            return amount;
        }

        public static double GetResourceMax(Vessel v, string resourceName)
        {
            PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(resourceName);
            double amount = 0;
            foreach (Part mypart in v.parts)
            {
                if (mypart.Resources.Contains(resourceName))
                {
                    amount += mypart.Resources.Get(resource.id).maxAmount;
                }
            }
            return amount;
        }

        public static double GetPitch(Vessel thisVessel)
        {
            var pitch = Vector3d.Angle(FlightGlobals.upAxis, thisVessel.transform.up);
            return pitch;
        }

        public static int GetResourceID(this Part part, string resourceName)
        {
            PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(resourceName);
            return resource.id;
        }

        public static double GetResourceAmount(this Part part, string resourceName)
        {
            PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(resourceName);
            return part.Resources.Get(resource.id).amount;
        }

        public static double GetResourceSpace(this Part part, string resourceName)
        {
            PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(resourceName);
            double amt = part.Resources.Get(resource.id).amount;
            double max = part.Resources.Get(resource.id).maxAmount;
            return max - amt;
        }

        public static double findAltitude(Transform aLocation)
        {
            if (FlightGlobals.ActiveVessel == null) return 0;
            return Vector3.Distance(aLocation.position, FlightGlobals.ActiveVessel.mainBody.position) - (FlightGlobals.ActiveVessel.mainBody.Radius);
        }

        public static double DegreeBearing(Vessel v,
            double lat1, double lon1)
        {
            double R = v.orbitDriver.orbit.referenceBody.Radius;//Radius of current body

            var dLon = ToRad(lon1 - v.longitude);
            var dPhi = Math.Log(
                Math.Tan(ToRad(lat1) / 2 + Math.PI / 4) / Math.Tan(ToRad(v.latitude) / 2 + Math.PI / 4));
            if (Math.Abs(dLon) > Math.PI)
                dLon = dLon > 0 ? -(2 * Math.PI - dLon) : (2 * Math.PI + dLon);
            return ToBearing(Math.Atan2(dLon, dPhi));
        }

        public static string GetBody()
        {
            var body = FlightGlobals.ActiveVessel.orbitDriver.referenceBody.GetName();
            if (body == null)
            {
                body = "None";
            }
            return body;
        }

        public static float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float v321 = p3.x * p2.y * p1.z;
            float v231 = p2.x * p3.y * p1.z;
            float v312 = p3.x * p1.y * p2.z;
            float v132 = p1.x * p3.y * p2.z;
            float v213 = p2.x * p1.y * p3.z;
            float v123 = p1.x * p2.y * p3.z;

            return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
        }

        public static float VolumeOfMesh(Mesh mesh)
        {
            float volume = 0;

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                Vector3 p1 = vertices[triangles[i + 0]];
                Vector3 p2 = vertices[triangles[i + 1]];
                Vector3 p3 = vertices[triangles[i + 2]];
                volume += SignedVolumeOfTriangle(p1, p2, p3);
            }

            return Mathf.Abs(volume);
        }

        public static double GetGforce(Vessel v)
        {
            return FlightGlobals.getGeeForceAtPosition(v.CoM).magnitude;
        }

        public static double ToRad(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        public static double ToDegrees(double radians)
        {
            return radians * 180 / Math.PI;
        }

        public static double ToBearing(double radians)
        {
            // convert radians to degrees (as bearing: 0...360)
            return (ToDegrees(radians) + 360) % 360;
        }

        public static double CalculateDistance(Vessel v, double DesLat, double DesLong)
        {
            double PlanetRadius = v.orbitDriver.orbit.referenceBody.Radius;
            double deltaLat = ToRad(DesLat - v.latitude);
            double deltaLong = ToRad(DesLong - v.longitude);
            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) + Math.Cos(v.latitude * Math.PI / 180) * Math.Cos(DesLat * Math.PI / 180) * Math.Sin(deltaLong / 2) * Math.Sin(deltaLong / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var Distance = (PlanetRadius * c);

            return Distance;
        }

        public static float GetHeading(Vessel vessel)
        {
            var up = vessel.upAxis;
            var north = GetNorthVector(vessel);
            var myheading =
                Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vessel.GetTransform().rotation) *
                                   Quaternion.LookRotation(north, up));

            return myheading.eulerAngles.y;
        }

        public static Vector3d GetNorthVector(Vessel vessel)
        {
            return Vector3d.Exclude(vessel.upAxis, vessel.mainBody.transform.up);
        }

        public static float MStoKTS(float m)
        {
            m *= 1.94384f;
            return m;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        public static double GetLat(Vessel v, int which)
        {
            double lat = 0.00;
            switch (which)
            {
                case 1:
                    lat = Math.Round(v.latitude, 2);
                    break;
                case 2:
                    lat = v.latitude;
                    break;
            }

            //var lat = ((vessel.latitude + 90 + 180) % 180 - 90) * Mathf.Deg2Rad;
            //var lat = Math.Round(((vessel.mainBody.GetLatitude(vessel.findLocalCenterOfMass()) + 90 + 180) % 180 - 90) * Mathf.Deg2Rad,2);
            return lat;
        }

        public static double GetLong(Vessel v, int which)
        {
            double thislong = 0.00;
            switch (which)
            {
                case 1:
                    thislong = Math.Round(((v.longitude + 180) % 360) - 180, 2);
                    break;
                case 2:
                    thislong = v.longitude;
                    break;
            }
            //var thislong = Math.Round((((vessel.mainBody.GetLongitude(vessel.findWorldCenterOfMass()) + 180 + 360) % 360 - 180) * Mathf.Deg2Rad), 2);
            //var thislong = ((vessel.longitude + 180 + 360) % 360 - 180) * Mathf.Deg2Rad;
            return thislong;
        }

        public static float GetRnd()
        {
            System.Random r = new System.Random();
            float rand = r.Next(-20, 20);
            return rand;
        }

        public static string DepthBelowKeel(Vessel v)
        {
            if (Math.Abs(v.terrainAltitude) > 1000)
            {
                return "> 1000 m";
            }
            else
            {
                return Math.Round(v.altitude - v.terrainAltitude, 2).ToString() + " m";
            }
        }
    }
}

