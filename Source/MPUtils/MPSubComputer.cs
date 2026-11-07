/*********************************************************************************************
* The Maritime Pack MPUtil plugin is copyright 2015 Fengist, all rights reserved.
* For full license information please visit http://www.kerbaltopia.com
*********************************************************************************************/

using System;
using UnityEngine;

namespace MPUtils
{
    class MPSubComputer : PartModule
    {
        public static bool guiActive = false;
        private Part Part1;
        private Part Part2;
        private float keelcheck = 2.0f;
        private float buoycheck = 2.0f;
        private float batterycheck = -1.0f;
        private double transferamount = 0.01d;
        private double alt1, alt2;
        private double lastpitch = -1;
        private int rescount = 0;
        private int id;
        private int batid;
        private float vesselvolume = 0.0f;

        public void Start()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                foreach (Part mypart in this.vessel.parts)
                {
                    if (mypart.Resources.Contains("CompressedWater"))
                    {
                        if (rescount == 0)
                        { Part1 = mypart; }
                        if (rescount == 1)
                        { Part2 = mypart; }
                        rescount++;
                    }
                }
                MPLog.Writelog("[Maritime Pack] Compressed Water Found: " + rescount);
                id = MPFunctions.GetResourceID(Part1, "CompressedWater");
                batid = MPFunctions.GetResourceID(Part1, "ElectricCharge");
                vesselvolume = MPFunctions.GetVolume(this.vessel);
            }
        }


        [KSPEvent(guiActive = true, guiName = "Activate AutoKeel")]
        public void KeelActivate()
        {
            Events["KeelActivate"].active = false;
            Events["KeelDeactivate"].active = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate AutoKeel", active = false)]
        public void KeelDeactivate()
        {
            Events["KeelActivate"].active = true;
            Events["KeelDeactivate"].active = false;
        }

        [KSPEvent(guiActive = true, guiName = "Activate AutoBuoy")]
        public void BuoyActivate()
        {
            Events["BuoyActivate"].active = false;
            Events["BuoyDeactivate"].active = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate AutoBuoy", active = false)]
        public void BuoyDeactivate()
        {
            Events["BuoyActivate"].active = true;
            Events["BuoyDeactivate"].active = false;
        }


        [KSPEvent(guiActive = true, guiName = "Blow Ballast")]
        public void BlowBallast()
        {
            int id = MPFunctions.GetResourceID(Part1, "CompressedWater");
            Part1.TransferResource(id, -MPFunctions.GetResourceAmount(Part1, "CompressedWater"));
            Part2.TransferResource(id, -MPFunctions.GetResourceAmount(Part2, "CompressedWater"));
        }

        [KSPEvent(guiActive = true, guiName = "Show Info")]
        public void InfoActivate()
        {
            Events["InfoActivate"].active = false;
            Events["InfoDeactivate"].active = true;
            guiActive = true;
        }

        [KSPEvent(guiActive = true, guiName = "Hide Info", active = false)]
        public void InfoDeactivate()
        {
            Events["InfoActivate"].active = true;
            Events["InfoDeactivate"].active = false;
            guiActive = false;
        }


        public override void OnUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight && rescount != 2) return;
            {
                //Computer Power Drain
                if (Events["BuoyDeactivate"].active || Events["KeelDeactivate"].active || Events["InfoDeactivate"].active)
                {
                    
                    float rConsume = 0;
                    if (Events["BuoyDeactivate"].active)
                    { rConsume++; }
                    if (Events["KeelDeactivate"].active)
                    { rConsume++; }
                    if (Events["InfoDeactivate"].active)
                    { rConsume+=0.5f; }

                    
                    batterycheck -= Time.deltaTime;
                    bool goodcheck = false;
                    if (batterycheck < 0)
                    {
                        double rFlow = 0.0f;
                        Part rPart = MPFunctions.GetResourcePart(FlightGlobals.ActiveVessel, "ElectricCharge");
                        double rTotal = MPFunctions.GetResourceTotal(FlightGlobals.ActiveVessel, "ElectricCharge");
                        if (rPart != null && rTotal >= rConsume)
                        {
                            rFlow = rPart.RequestResource(batid,rConsume,ResourceFlowMode.ALL_VESSEL);
                            goodcheck = true;
                        }
                        if (!goodcheck || rFlow != rConsume || rTotal < rConsume)
                        {
                            Events["BuoyDeactivate"].active = false;
                            Events["BuoyActivate"].active = true;
                            Events["KeelActivate"].active = true;
                            Events["KeelDeactivate"].active = false;
                            Events["InfoActivate"].active = true;
                            Events["InfoDeactivate"].active = false;
                            MPLog.Writelog("[Maritime Pack] Sub Computer: Battery depleted.");
                        }
                        batterycheck = 10.0f;
                    }
                }
                //AutoBuoyancy
                if (Events["BuoyDeactivate"].active == true)
                {
                    buoycheck -= Time.deltaTime;
                    if (buoycheck < 0)
                    {
                        buoycheck = 2.0f;
                        double vesselspeed = (double)Decimal.Round((decimal)this.vessel.verticalSpeed, 3);
                        MPLog.Writelog("[Maritime Pack] Vessel Vertical Speed: " + vesselspeed);
                        transferamount = (double)decimal.Round((decimal)vesselspeed, 2) / 2;
                        if (vesselspeed < 0)
                        {
                            MPLog.Writelog("[Maritime Pack] Removing buoy: " + transferamount);
                            double p1amount = MPFunctions.GetResourceAmount(Part1, "CompressedWater");
                            double p2amount = MPFunctions.GetResourceAmount(Part2, "CompressedWater");
                            if (p1amount > transferamount)
                            {
                                Part1.TransferResource(id, transferamount);
                            }
                            else if (p1amount > 0)
                            {
                                Part1.TransferResource(id, -p1amount);
                            }
                            if (p2amount > transferamount)
                            {
                                Part2.TransferResource(id, transferamount);
                            }
                            else if (p1amount > 0)
                            {
                                Part2.TransferResource(id, -p2amount);
                            }
                        }
                        else if (vesselspeed > 0)
                        {
                            MPLog.Writelog("[Maritime Pack] Adding buoy: " + transferamount);
                            double p1amount = MPFunctions.GetResourceSpace(Part1, "CompressedWater");
                            double p2amount = MPFunctions.GetResourceSpace(Part2, "CompressedWater");
                            if (p1amount > transferamount)
                            {
                                Part1.TransferResource(id, transferamount);
                            }
                            else if (p1amount > 0)
                            {
                                Part1.TransferResource(id, p1amount);
                            }
                            if (p2amount > transferamount)
                            {
                                Part2.TransferResource(id, transferamount);
                            }
                            else if (p1amount > 0)
                            {
                                Part2.TransferResource(id, p2amount);
                            }
                        }
                        else
                        {
                            Events["BuoyDeactivate"].active = false;
                            Events["BuoyActivate"].active = true;
                        }
                    }
                }

                //AutoKeel
                if (Events["KeelDeactivate"].active == true)
                {
                    keelcheck -= Time.deltaTime;
                    if (keelcheck < 0)
                    {
                        bool skip = false;
                        double pitch = Math.Abs(MPFunctions.GetPitch(this.vessel) - 90);
                        double realpitch = MPFunctions.GetPitch(this.vessel);
                        if (lastpitch != -1)
                        {
                            double deltapitch = Math.Abs(lastpitch - pitch);
                            MPLog.Writelog("[Maritime Pack] AutoKeel: DeltaPitch: " + deltapitch);
                            if (deltapitch > 10)
                            {
                                skip = true;
                                keelcheck = 10.0f;
                            }
                            else if (deltapitch < 3)
                            {
                                keelcheck = 7.0f;
                            }
                            else
                            {
                                keelcheck = (10 - (float)deltapitch);
                            }
                        }
                        else
                        {
                            keelcheck = 3.0f;
                            this.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
                        }
                        lastpitch = pitch;
                        if (!skip)
                        {
                            transferamount = ((.20 - .01) / (90 - 30)) * pitch;
                            if (transferamount < 0.01f)
                            { transferamount = 0.01f; }
                            MPLog.Writelog("[Maritime Pack] Pitch: " + pitch);
                            alt1 = MPFunctions.findAltitude(Part1.transform);
                            alt2 = MPFunctions.findAltitude(Part2.transform);
                            MPLog.Writelog("[Maritime Pack] Autokeel Running: Alt1:" + alt1 + " Alt2:" + alt2 + " Amt: " + transferamount);
                            if (Math.Abs(alt1 - alt2) < 1.0f)
                            {
                                MPLog.Writelog("[Maritime Pack] AutoKeel: Shut Down: " + alt1 + " " + alt2);
                                Events["KeelActivate"].active = true;
                                Events["KeelDeactivate"].active = false;
                                this.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
                            }
                            else if (alt1 > alt2)
                            {
                                if (MPFunctions.GetResourceAmount(Part2, "CompressedWater") > transferamount && MPFunctions.GetResourceSpace(Part1, "CompressedWater") > transferamount)
                                {
                                    Part2.TransferResource(id, -transferamount);
                                    Part1.TransferResource(id, transferamount);
                                    MPLog.Writelog("[Maritime Pack] Moving " + transferamount + " to "+Part1.name);
                                }
                                else
                                {
                                    MPLog.Writelog("[Maritime Pack] AutoKeel: Unable to transfer resources.");
                                    Events["KeelActivate"].active = true;
                                    Events["KeelDeactivate"].active = false;
                                }
                            }
                            else if (alt1 < alt2)
                            {
                                if (MPFunctions.GetResourceAmount(Part1, "CompressedWater") > transferamount && MPFunctions.GetResourceSpace(Part2, "CompressedWater") > transferamount)
                                {
                                    Part1.TransferResource(id, -transferamount);
                                    Part2.TransferResource(id, transferamount);
                                    MPLog.Writelog("[Maritime Pack] Moving "+transferamount+" to "+Part2.name);
                                }
                                else
                                {
                                    MPLog.Writelog("[Maritime Pack] AutoKeel: Unable to transfer resources.");
                                    Events["KeelActivate"].active = true;
                                    Events["KeelDeactivate"].active = false;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
