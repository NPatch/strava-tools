using Dynastream.Fit;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using SysDateTime = System.DateTime;

namespace StravaTools.Helpers
{
    public static class FitHelper
    {
        public static string ConvertLocal(this SysDateTime dt, uint val)
        {
            SysDateTime _dt = dt;
            _dt.AddSeconds(val);
            return _dt.ToString();
        }

        public static void Encode(FileInfo dest, ref FitMessages fitMessages)
        {
            // Create file encode object
            Encode encoder = new Encode(ProtocolVersion.V20);

            //encoder.header.Size = 12;

            // Write our header
            FileStream output = new FileStream(dest.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            encoder.Open(output);

            // Encode each message, a definition message is automatically generated and output if necessary
            encoder.Write(fitMessages.FileIdMesgs);
            encoder.Write(fitMessages.ActivityMesgs);
            encoder.Write(fitMessages.SessionMesgs);
            encoder.Write(fitMessages.LapMesgs);
            encoder.Write(fitMessages.RecordMesgs);

            encoder.Close();
            output.Close();
        }

        public static FitMessages Decode(FileInfo src)
        {
            FileStream fitSource = new FileStream(src.FullName, FileMode.Open, FileAccess.Read);
            Decode decoder = new Decode();
            FitListener fitListener = new FitListener();
            decoder.MesgEvent += fitListener.OnMesg;         
            decoder.Read(fitSource);
            FitMessages fitMessages = fitListener.FitMessages;
            fitSource.Close();
            return fitMessages;
        }

        public static bool DoesSessionContainSteps(ref FitMessages fitMessages)
        {
            return (fitMessages.SessionMesgs[0].GetTotalCycles().HasValue)
                || (fitMessages.SessionMesgs[0].GetTotalCycles().HasValue
                    && fitMessages.SessionMesgs[0].GetTotalCycles().Value > 0);
        }

        public static bool CheckIntegrity(FileInfo dest)
        {
            Decode decoder = new Decode();
            FileStream fs = new FileStream(dest.FullName, FileMode.Open, FileAccess.Read);
            bool is_output_valid = decoder.CheckIntegrity(fs);
            fs.Close();
            return is_output_valid;
        }

        public static void PrintFileIdMesg(FileIdMesg mesg)
        {
            Log.Information("File ID:");

            if (mesg.GetType() != null)
            {
                Log.Information($"   Type: {mesg.GetType().Value.ToString()}");
            }

            if (mesg.GetManufacturer() != null)
            {
                Log.Information($"   Manufacturer: {mesg.GetManufacturer().ToString()}");
            }

            if (mesg.GetProduct() != null)
            {
                Log.Information($"   Product: {mesg.GetProduct().ToString()}");
            }

            if (mesg.GetSerialNumber() != null)
            {
                Log.Information($"   Serial Number: {mesg.GetSerialNumber().ToString()}");
            }

            if (mesg.GetTimeCreated() != null)
            {
                SysDateTime dt = mesg.GetTimeCreated().GetDateTime();
                Log.Information($"   Time Created: {dt.ToString()}");
            }

            if (mesg.GetNumber() != null)
            {
                Log.Information($"   Number: {mesg.GetNumber().ToString()}");
            }

            Log.Information("");
        }

        public static void PrintActivityMesg(ActivityMesg mesg, SysDateTime time_created)
        {
            Log.Information("Activity:");

            if (mesg.GetTimestamp() != null)
            {
                Log.Information($"   Timestamp: {mesg.GetTimestamp().GetDateTime().ToString()}");
            }

            if (mesg.GetTotalTimerTime() != null)
            {
                Log.Information($"   TotalTimerTime: {mesg.GetTotalTimerTime().Value.ToString()}");
            }

            if (mesg.GetNumSessions() != null)
            {
                Log.Information($"   NumSessions: {mesg.GetNumSessions().ToString()}");
            }

            if (mesg.GetType() != null)
            {
                Log.Information($"   Type: {mesg.GetType().Value.ToString()}");
            }

            if (mesg.GetEvent() != null)
            {
                Log.Information($"   Event: {mesg.GetEvent().Value.ToString()}");
            }

            if (mesg.GetEventType() != null)
            {
                Log.Information($"   EventType: {mesg.GetEventType().Value.ToString()}");
            }

            if (mesg.GetLocalTimestamp() != null)
            {
                Log.Information($"   LocalTimestamp: {time_created.ConvertLocal(mesg.GetLocalTimestamp().Value).ToString()}");
            }

            if (mesg.GetEventGroup() != null)
            {
                Log.Information($"   EventGroup: {mesg.GetEventGroup().ToString()}");
            }

            Log.Information("");
        }

        public static void PrintSessionMesg(SessionMesg mesg)
        {
            Log.Information("Session:");

            // 0
            if (mesg.GetEvent() != null)
                Log.Information("    Event: {0}", mesg.GetEvent().ToString());

            // 1
            if (mesg.GetEventType() != null)
                Log.Information("    Event Type: {0}", mesg.GetEventType().ToString());

            // 2
            if (mesg.GetStartTime() != null)
                Log.Information("    Start Time: {0}", mesg.GetStartTime().GetDateTime().ToString());

            // 3
            if (mesg.GetStartPositionLat() != null)
                Log.Information("    Start Position Lat: {0}", mesg.GetStartPositionLat());

            // 4
            if (mesg.GetStartPositionLong() != null)
                Log.Information("    Start Position Long: {0}", mesg.GetStartPositionLong());

            // 5
            if (mesg.GetSport() != null)
                Log.Information("    Sport: {0}", mesg.GetSport().ToString());

            // 6
            if (mesg.GetSubSport() != null)
                Log.Information("    SubSport: {0}", mesg.GetSubSport().ToString());

            // 7
            if (mesg.GetTotalElapsedTime() != null)
                Log.Information("    Total Elapsed Time: {0}", mesg.GetTotalElapsedTime());

            // 8
            if (mesg.GetTotalTimerTime() != null)
                Log.Information("    Total Timer Time: {0}", mesg.GetTotalTimerTime());

            // 9
            if (mesg.GetTotalDistance() != null)
                Log.Information("    Total Distance: {0}", mesg.GetTotalDistance());

            // 10
            if (mesg.GetTotalCycles() != null)
                Log.Information("    Total Cycles: {0}", mesg.GetTotalCycles());

            // 11
            if (mesg.GetTotalCalories() != null)
                Log.Information("    Total Calories: {0}", mesg.GetTotalCalories());

            // 13
            if (mesg.GetTotalFatCalories() != null)
                Log.Information("    Total Fat Calories: {0}", mesg.GetTotalFatCalories());

            // 14
            if (mesg.GetAvgSpeed() != null)
                Log.Information("    Avg Speed: {0}", mesg.GetAvgSpeed());

            // 15
            if (mesg.GetMaxSpeed() != null)
                Log.Information("    Max Speed: {0}", mesg.GetMaxSpeed());

            // 16
            if (mesg.GetAvgHeartRate() != null)
                Log.Information("    Avg HR: {0}", mesg.GetAvgHeartRate());

            // 17
            if (mesg.GetMaxHeartRate() != null)
                Log.Information("    Max HR: {0}", mesg.GetMaxHeartRate());

            // 18
            if (mesg.GetAvgCadence() != null)
                Log.Information("    Avg Cadence: {0}", mesg.GetAvgCadence());

            // 19
            if (mesg.GetMaxCadence() != null)
                Log.Information("    Max Cadence: {0}", mesg.GetMaxCadence());

            // 20
            if (mesg.GetAvgPower() != null)
                Log.Information("    Avg Power: {0}", mesg.GetAvgPower());

            // 21
            if (mesg.GetMaxPower() != null)
                Log.Information("    Max Power: {0}", mesg.GetMaxPower());

            // 22
            if (mesg.GetTotalAscent() != null)
                Log.Information("    Total Ascent: {0}", mesg.GetTotalAscent());

            // 23
            if (mesg.GetTotalDescent() != null)
                Log.Information("    Total Descent: {0}", mesg.GetTotalDescent());

            // 24
            if (mesg.GetTotalTrainingEffect() != null)
                Log.Information("    Total Training Effect: {0}", mesg.GetTotalTrainingEffect());

            // 25
            if (mesg.GetFirstLapIndex() != null)
                Log.Information("    First Lap Index: {0}", mesg.GetFirstLapIndex());

            // 26
            if (mesg.GetNumLaps() != null)
                Log.Information("    Number of Laps: {0}", mesg.GetNumLaps());

            // 27
            if (mesg.GetEventGroup() != null)
                Log.Information("    Event Group: {0}", mesg.GetEventGroup());

            // 28
            if (mesg.GetTrigger() != null)
                Log.Information("    Trigger: {0}", mesg.GetTrigger().ToString());

            // 29
            if (mesg.GetNecLat() != null)
                Log.Information("    NEC Lat: {0}", mesg.GetNecLat());

            // 30
            if (mesg.GetNecLong() != null)
                Log.Information("    NEC Long: {0}", mesg.GetNecLong());

            // 31
            if (mesg.GetSwcLat() != null)
                Log.Information("    SWC Lat: {0}", mesg.GetSwcLat());

            // 32
            if (mesg.GetSwcLong() != null)
                Log.Information("    SWC Long: {0}", mesg.GetSwcLong());

            // 33
            if (mesg.GetNumLengths() != null)
                Log.Information("    Number of Lengths: {0}", mesg.GetNumLengths());

            // 34
            if (mesg.GetNormalizedPower() != null)
                Log.Information("    Normalized Power: {0}", mesg.GetNormalizedPower());

            // 35
            if (mesg.GetTrainingStressScore() != null)
                Log.Information("    Training Stress Score: {0}", mesg.GetTrainingStressScore());

            // 36
            if (mesg.GetIntensityFactor() != null)
                Log.Information("    Intensity Factor: {0}", mesg.GetIntensityFactor());

            // 37
            if (mesg.GetLeftRightBalance() != null)
                Log.Information("    Left/Right Balance: {0}", mesg.GetLeftRightBalance().ToString());

            // 38
            if (mesg.GetEndPositionLat() != null)
                Log.Information("    End Position Lat: {0}", mesg.GetEndPositionLat());

            // 39
            if (mesg.GetEndPositionLong() != null)
                Log.Information("    End Position Long: {0}", mesg.GetEndPositionLong());

            // 41
            if (mesg.GetAvgStrokeCount() != null)
                Log.Information("    Avg Stroke Count: {0}", mesg.GetAvgStrokeCount());

            // 42
            if (mesg.GetAvgStrokeDistance() != null)
                Log.Information("    Avg Stroke Distance: {0}", mesg.GetAvgStrokeDistance());

            // 43
            if (mesg.GetSwimStroke() != null)
                Log.Information("    Swim Stroke: {0}", mesg.GetSwimStroke().ToString());

            // 44
            if (mesg.GetPoolLength() != null)
                Log.Information("    Pool Length: {0}", mesg.GetPoolLength());

            // 45
            if (mesg.GetThresholdPower() != null)
                Log.Information("    Threshold Power: {0}", mesg.GetThresholdPower());

            // 46
            if (mesg.GetPoolLengthUnit() != null)
                Log.Information("    Pool Length Unit: {0}", mesg.GetPoolLengthUnit().ToString());

            // 47
            if (mesg.GetNumActiveLengths() != null)
                Log.Information("    Number of Active Lengths: {0}", mesg.GetNumActiveLengths());

            // 48
            if (mesg.GetTotalWork() != null)
                Log.Information("    Total Work: {0}", mesg.GetTotalWork());

            // 49
            if (mesg.GetAvgAltitude() != null)
                Log.Information("    Avg Altitude: {0}", mesg.GetAvgAltitude());

            // 50
            if (mesg.GetMaxAltitude() != null)
                Log.Information("    Max Altitude: {0}", mesg.GetMaxAltitude());

            // 51
            if (mesg.GetGpsAccuracy() != null)
                Log.Information("    GPS Accuracy: {0}", mesg.GetGpsAccuracy());

            // 52
            if (mesg.GetAvgGrade() != null)
                Log.Information("    Avg Grade: {0}", mesg.GetAvgGrade());

            // 53
            if (mesg.GetAvgPosGrade() != null)
                Log.Information("    Avg Positive Grade: {0}", mesg.GetAvgPosGrade());

            // 54
            if (mesg.GetAvgNegGrade() != null)
                Log.Information("    Avg Negative Grade: {0}", mesg.GetAvgNegGrade());

            // 55
            if (mesg.GetMaxPosGrade() != null)
                Log.Information("    Max Positive Grade: {0}", mesg.GetMaxPosGrade());

            // 56
            if (mesg.GetMaxNegGrade() != null)
                Log.Information("    Max Negative Grade: {0}", mesg.GetMaxNegGrade());

            // 57
            if (mesg.GetAvgTemperature() != null)
                Log.Information("    Avg Temperature: {0}", mesg.GetAvgTemperature());

            // 58
            if (mesg.GetMaxTemperature() != null)
                Log.Information("    Max Temperature: {0}", mesg.GetMaxTemperature());

            // 59
            if (mesg.GetTotalMovingTime() != null)
                Log.Information("    Total Moving Time: {0}", mesg.GetTotalMovingTime());

            // 60
            if (mesg.GetAvgPosVerticalSpeed() != null)
                Log.Information("    Avg Positive Vertical Speed: {0}", mesg.GetAvgPosVerticalSpeed());

            // 61
            if (mesg.GetAvgNegVerticalSpeed() != null)
                Log.Information("    Avg Negative Vertical Speed: {0}", mesg.GetAvgNegVerticalSpeed());

            // 62
            if (mesg.GetMaxPosVerticalSpeed() != null)
                Log.Information("    Max Positive Vertical Speed: {0}", mesg.GetMaxPosVerticalSpeed());

            // 63
            if (mesg.GetMaxNegVerticalSpeed() != null)
                Log.Information("    Max Negative Vertical Speed: {0}", mesg.GetMaxNegVerticalSpeed());

            // 64
            if (mesg.GetMinHeartRate() != null)
                Log.Information("    Min HR: {0}", mesg.GetMinHeartRate());

            // 65 (array)
            for (int i = 0; i < mesg.GetNumTimeInHrZone(); i++)
                Log.Information("    Time in HR Zone[{0}]: {0}", mesg.GetTimeInHrZone(i));

            // 66 (array)
            for (int i = 0; i < mesg.GetNumTimeInSpeedZone(); i++)
                Log.Information("    Time in Speed Zone[{0}]: {1}", i, mesg.GetTimeInSpeedZone(i));

            // 67 (array)
            for (int i = 0; i < mesg.GetNumTimeInCadenceZone(); i++)
                Log.Information("    Time in Cadence Zone[{0}]: {1}", i, mesg.GetTimeInCadenceZone(i));

            // 68 (array)
            for (int i = 0; i < mesg.GetNumTimeInPowerZone(); i++)
                Log.Information("    Time in Power Zone[{0}]: {1}", i, mesg.GetTimeInPowerZone(i));

            // 69
            if (mesg.GetAvgLapTime() != null)
                Log.Information("    Avg Lap Time: {0}", mesg.GetAvgLapTime());

            // 70
            if (mesg.GetBestLapIndex() != null)
                Log.Information("    Best Lap Index: {0}", mesg.GetBestLapIndex());

            // 71
            if (mesg.GetMinAltitude() != null)
                Log.Information("    Min Altitude: {0}", mesg.GetMinAltitude());

            // 82
            if (mesg.GetPlayerScore() != null)
                Log.Information("    Player Score: {0}", mesg.GetPlayerScore());

            // 83
            if (mesg.GetOpponentScore() != null)
                Log.Information("    Opponent Score: {0}", mesg.GetOpponentScore());

            // 84
            if (!string.IsNullOrEmpty(mesg.GetOpponentNameAsString()))
                Log.Information("    Opponent Name: {0}", mesg.GetOpponentNameAsString());

            // 85 (array)
            for (int i = 0; i < mesg.GetNumStrokeCount(); i++)
                Log.Information("    Stroke Count[{0}]: {1}", i, mesg.GetStrokeCount(i));

            // 86 (array)
            for (int i = 0; i < mesg.GetNumZoneCount(); i++)
                Log.Information("    Zone Count[{0}]: {1}", i, mesg.GetZoneCount(i));

            // 87
            if (mesg.GetMaxBallSpeed() != null)
                Log.Information("    Max Ball Speed: {0}", mesg.GetMaxBallSpeed());

            // 88
            if (mesg.GetAvgBallSpeed() != null)
                Log.Information("    Avg Ball Speed: {0}", mesg.GetAvgBallSpeed());

            // 89
            if (mesg.GetAvgVerticalOscillation() != null)
                Log.Information("    Avg Vertical Oscillation: {0}", mesg.GetAvgVerticalOscillation());

            // 90
            if (mesg.GetAvgStanceTimePercent() != null)
                Log.Information("    Avg Stance Time Percent: {0}", mesg.GetAvgStanceTimePercent());

            // 91
            if (mesg.GetAvgStanceTime() != null)
                Log.Information("    Avg Stance Time: {0}", mesg.GetAvgStanceTime());

            // 92
            if (mesg.GetAvgFractionalCadence() != null)
                Log.Information("    Avg Fractional Cadence: {0}", mesg.GetAvgFractionalCadence());

            // 93
            if (mesg.GetMaxFractionalCadence() != null)
                Log.Information("    Max Fractional Cadence: {0}", mesg.GetMaxFractionalCadence());

            // 94
            if (mesg.GetTotalFractionalCycles() != null)
                Log.Information("    Total Fractional Cycles: {0}", mesg.GetTotalFractionalCycles());

            // 95 (array)
            for (int i = 0; i < mesg.GetNumAvgTotalHemoglobinConc(); i++)
                Log.Information("    Avg Total Hb Conc[{0}]: {1}", i, mesg.GetAvgTotalHemoglobinConc(i));

            // 96 (array)
            for (int i = 0; i < mesg.GetNumMinTotalHemoglobinConc(); i++)
                Log.Information("    Min Total Hb Conc[{0}]: {1}", i, mesg.GetMinTotalHemoglobinConc(i));

            // 97 (array)
            for (int i = 0; i < mesg.GetNumMaxTotalHemoglobinConc(); i++)
                Log.Information("    Max Total Hb Conc[{0}]: {1}", i, mesg.GetMaxTotalHemoglobinConc(i));

            // 98 (array)
            for (int i = 0; i < mesg.GetNumAvgSaturatedHemoglobinPercent(); i++)
                Log.Information("    Avg Hb Saturation[{0}]: {1}", i, mesg.GetAvgSaturatedHemoglobinPercent(i));

            // 99 (array)
            for (int i = 0; i < mesg.GetNumMinSaturatedHemoglobinPercent(); i++)
                Log.Information("    Min Hb Saturation[{0}]: {1}", i, mesg.GetMinSaturatedHemoglobinPercent(i));

            // 100 (array)
            for (int i = 0; i < mesg.GetNumMaxSaturatedHemoglobinPercent(); i++)
                Log.Information("    Max Hb Saturation[{0}]: {1}", i, mesg.GetMaxSaturatedHemoglobinPercent(i));

            // 101
            if (mesg.GetAvgLeftTorqueEffectiveness() != null)
                Log.Information("    Avg Left Torque Effectiveness: {0}", mesg.GetAvgLeftTorqueEffectiveness());

            // 102
            if (mesg.GetAvgRightTorqueEffectiveness() != null)
                Log.Information("    Avg Right Torque Effectiveness: {0}", mesg.GetAvgRightTorqueEffectiveness());

            // 103
            if (mesg.GetAvgLeftPedalSmoothness() != null)
                Log.Information("    Avg Left Pedal Smoothness: {0}", mesg.GetAvgLeftPedalSmoothness());

            // 104
            if (mesg.GetAvgRightPedalSmoothness() != null)
                Log.Information("    Avg Right Pedal Smoothness: {0}", mesg.GetAvgRightPedalSmoothness());

            // 105
            if (mesg.GetAvgCombinedPedalSmoothness() != null)
                Log.Information("    Avg Combined Pedal Smoothness: {0}", mesg.GetAvgCombinedPedalSmoothness());

            // 110
            if (!string.IsNullOrEmpty(mesg.GetSportProfileNameAsString()))
                Log.Information("    Sport Profile Name: {0}", mesg.GetSportProfileNameAsString());

            // 111
            if (mesg.GetSportIndex() != null)
                Log.Information("    Sport Index: {0}", mesg.GetSportIndex());

            // 112
            if (mesg.GetTimeStanding() != null)
                Log.Information("    Time Standing: {0}", mesg.GetTimeStanding());

            // 113
            if (mesg.GetStandCount() != null)
                Log.Information("    Stand Count: {0}", mesg.GetStandCount());

            // 114
            if (mesg.GetAvgLeftPco() != null)
                Log.Information("    Avg Left PCO: {0}", mesg.GetAvgLeftPco());

            // 115
            if (mesg.GetAvgRightPco() != null)
                Log.Information("    Avg Right PCO: {0}", mesg.GetAvgRightPco());

            // 116 (array)
            for (int i = 0; i < mesg.GetNumAvgLeftPowerPhase(); i++)
                Log.Information("    Avg Left Power Phase[{0}]: {1}", i, mesg.GetAvgLeftPowerPhase(i));

            // 117 (array)
            for (int i = 0; i < mesg.GetNumAvgLeftPowerPhasePeak(); i++)
                Log.Information("    Avg Left Power Phase Peak[{0}]: {1}", i, mesg.GetAvgLeftPowerPhasePeak(i));

            // 118 (array)
            for (int i = 0; i < mesg.GetNumAvgRightPowerPhase(); i++)
                Log.Information("    Avg Right Power Phase[{0}]: {1}", i, mesg.GetAvgRightPowerPhase(i));

            // 119 (array)
            for (int i = 0; i < mesg.GetNumAvgRightPowerPhasePeak(); i++)
                Log.Information("    Avg Right Power Phase Peak[{0}]: {1}", i, mesg.GetAvgRightPowerPhasePeak(i));

            // 120 (array)
            for (int i = 0; i < mesg.GetNumAvgPowerPosition(); i++)
                Log.Information("    Avg Power Position[{0}]: {1}", i, mesg.GetAvgPowerPosition(i));

            // 121 (array)
            for (int i = 0; i < mesg.GetNumMaxPowerPosition(); i++)
                Log.Information("    Max Power Position[{0}]: {1}", i, mesg.GetMaxPowerPosition(i));

            // 122 (array)
            for (int i = 0; i < mesg.GetNumAvgCadencePosition(); i++)
                Log.Information("    Avg Cadence Position[{0}]: {1}", i, mesg.GetAvgCadencePosition(i));

            // 123 (array)
            for (int i = 0; i < mesg.GetNumMaxCadencePosition(); i++)
                Log.Information("    Max Cadence Position[{0}]: {1}", i, mesg.GetMaxCadencePosition(i));

            // 124
            if (mesg.GetEnhancedAvgSpeed() != null)
                Log.Information("    Enhanced Avg Speed: {0}", mesg.GetEnhancedAvgSpeed());

            // 125
            if (mesg.GetEnhancedMaxSpeed() != null)
                Log.Information("    Enhanced Max Speed: {0}", mesg.GetEnhancedMaxSpeed());

            // 126
            if (mesg.GetEnhancedAvgAltitude() != null)
                Log.Information("    Enhanced Avg Altitude: {0}", mesg.GetEnhancedAvgAltitude());

            // 127
            if (mesg.GetEnhancedMinAltitude() != null)
                Log.Information("    Enhanced Min Altitude: {0}", mesg.GetEnhancedMinAltitude());

            // 128
            if (mesg.GetEnhancedMaxAltitude() != null)
                Log.Information("    Enhanced Max Altitude: {0}", mesg.GetEnhancedMaxAltitude());

            // 129
            if (mesg.GetAvgLevMotorPower() != null)
                Log.Information("    Avg LEV Motor Power: {0}", mesg.GetAvgLevMotorPower());

            // 130
            if (mesg.GetMaxLevMotorPower() != null)
                Log.Information("    Max LEV Motor Power: {0}", mesg.GetMaxLevMotorPower());

            // 131
            if (mesg.GetLevBatteryConsumption() != null)
                Log.Information("    LEV Battery Consumption: {0}", mesg.GetLevBatteryConsumption());

            // 132
            if (mesg.GetAvgVerticalRatio() != null)
                Log.Information("    Avg Vertical Ratio: {0}", mesg.GetAvgVerticalRatio());

            // 133
            if (mesg.GetAvgStanceTimeBalance() != null)
                Log.Information("    Avg Stance Time Balance: {0}", mesg.GetAvgStanceTimeBalance());

            // 134
            if (mesg.GetAvgStepLength() != null)
                Log.Information("    Avg Step Length: {0}", mesg.GetAvgStepLength());

            // 137
            if (mesg.GetTotalAnaerobicTrainingEffect() != null)
                Log.Information("    Total Anaerobic Training Effect: {0}", mesg.GetTotalAnaerobicTrainingEffect());

            // 139
            if (mesg.GetAvgVam() != null)
                Log.Information("    Avg VAM: {0}", mesg.GetAvgVam());

            // 140
            if (mesg.GetAvgDepth() != null)
                Log.Information("    Avg Depth: {0}", mesg.GetAvgDepth());

            // 141
            if (mesg.GetMaxDepth() != null)
                Log.Information("    Max Depth: {0}", mesg.GetMaxDepth());

            // 142
            if (mesg.GetSurfaceInterval() != null)
                Log.Information("    Surface Interval: {0}", mesg.GetSurfaceInterval());

            // 143
            if (mesg.GetStartCns() != null)
                Log.Information("    Start CNS: {0}", mesg.GetStartCns());

            // 144
            if (mesg.GetEndCns() != null)
                Log.Information("    End CNS: {0}", mesg.GetEndCns());

            // 145
            if (mesg.GetStartN2() != null)
                Log.Information("    Start N2: {0}", mesg.GetStartN2());

            // 146
            if (mesg.GetEndN2() != null)
                Log.Information("    End N2: {0}", mesg.GetEndN2());

            // 147
            if (mesg.GetAvgRespirationRate() != null)
                Log.Information("    Avg Respiration Rate: {0}", mesg.GetAvgRespirationRate());

            // 148
            if (mesg.GetMaxRespirationRate() != null)
                Log.Information("    Max Respiration Rate: {0}", mesg.GetMaxRespirationRate());

            // 149
            if (mesg.GetMinRespirationRate() != null)
                Log.Information("    Min Respiration Rate: {0}", mesg.GetMinRespirationRate());

            // 150
            if (mesg.GetMinTemperature() != null)
                Log.Information("    Min Temperature: {0}", mesg.GetMinTemperature());

            // 155 (array)
            if (mesg.GetO2Toxicity() != null)
                Log.Information("    O2 Toxicity: {0}", mesg.GetO2Toxicity());

            // 156
            if (mesg.GetDiveNumber() != null)
                Log.Information("    Dive Number: {0}", mesg.GetDiveNumber());

            // 168
            if (mesg.GetTrainingLoadPeak() != null)
                Log.Information("    Training Load Peak: {0}", mesg.GetTrainingLoadPeak());

            // 169
            if (mesg.GetEnhancedAvgRespirationRate() != null)
                Log.Information("    Enhanced Avg Respiration Rate: {0}", mesg.GetEnhancedAvgRespirationRate());

            // 170
            if (mesg.GetEnhancedMaxRespirationRate() != null)
                Log.Information("    Enhanced Max Respiration Rate: {0}", mesg.GetEnhancedMaxRespirationRate());

            // 180
            if (mesg.GetEnhancedMinRespirationRate() != null)
                Log.Information("    Enhanced Min Respiration Rate: {0}", mesg.GetEnhancedMinRespirationRate());

            // 181
            if (mesg.GetTotalGrit() != null)
                Log.Information("    Total Grit: {0}", mesg.GetTotalGrit());

            // 182
            if (mesg.GetTotalFlow() != null)
                Log.Information("    Total Flow: {0}", mesg.GetTotalFlow());

            // 183
            if (mesg.GetJumpCount() != null)
                Log.Information("    Jump Count: {0}", mesg.GetJumpCount());

            // 186
            if (mesg.GetAvgGrit() != null)
                Log.Information("    Avg Grit: {0}", mesg.GetAvgGrit());

            // 187
            if (mesg.GetAvgFlow() != null)
                Log.Information("    Avg Flow: {0}", mesg.GetAvgFlow());

            // 192
            if (mesg.GetWorkoutFeel() != null)
                Log.Information("    Workout Feel: {0}", mesg.GetWorkoutFeel().ToString());

            // 193
            if (mesg.GetWorkoutRpe() != null)
                Log.Information("    Workout RPE: {0}", mesg.GetWorkoutRpe());

            // 194
            if (mesg.GetAvgSpo2() != null)
                Log.Information("    Avg SpO2: {0}", mesg.GetAvgSpo2());

            // 195
            if (mesg.GetAvgStress() != null)
                Log.Information("    Avg Stress: {0}", mesg.GetAvgStress());

            // 196
            if (mesg.GetMetabolicCalories() != null)
                Log.Information("    Metabolic Calories: {0}", mesg.GetMetabolicCalories());

            // 197
            if (mesg.GetSdrrHrv() != null)
                Log.Information("    SDRR HRV: {0}", mesg.GetSdrrHrv());

            // 198
            if (mesg.GetRmssdHrv() != null)
                Log.Information("    RMSSD HRV: {0}", mesg.GetRmssdHrv());

            // 199
            if (mesg.GetTotalFractionalAscent() != null)
                Log.Information("    Total Fractional Ascent: {0}", mesg.GetTotalFractionalAscent());

            // 200
            if (mesg.GetTotalFractionalDescent() != null)
                Log.Information("    Total Fractional Descent: {0}", mesg.GetTotalFractionalDescent());

            // 208
            if (mesg.GetAvgCoreTemperature() != null)
                Log.Information("    Avg Core Temperature: {0}", mesg.GetAvgCoreTemperature());

            // 209
            if (mesg.GetMinCoreTemperature() != null)
                Log.Information("    Min Core Temperature: {0}", mesg.GetMinCoreTemperature());

            // 210
            if (mesg.GetMaxCoreTemperature() != null)
                Log.Information("    Max Core Temperature: {0}", mesg.GetMaxCoreTemperature());

            // 253
            if (mesg.GetTimestamp() != null)
                Log.Information("    Timestamp: {0}", mesg.GetTimestamp().GetDateTime().ToString());

            // 254
            if (mesg.GetMessageIndex() != null)
                Log.Information("    Message Index: {0}", mesg.GetMessageIndex());

            Log.Information("");
        }

        public static void PrintLapMesg(LapMesg mesg)
        {
            Log.Information("Lap:");

            // 0
            if (mesg.GetEvent() != null)
                Log.Information("    Event: {0}", mesg.GetEvent().ToString());

            // 1
            if (mesg.GetEventType() != null)
                Log.Information("    Event Type: {0}", mesg.GetEventType().ToString());

            // 2
            if (mesg.GetStartTime() != null)
                Log.Information("    Start Time: {0}", mesg.GetStartTime().GetDateTime().ToString());

            // 3
            if (mesg.GetStartPositionLat() != null)
                Log.Information("    Start Position Lat: {0}", mesg.GetStartPositionLat());

            // 4
            if (mesg.GetStartPositionLong() != null)
                Log.Information("    Start Position Long: {0}", mesg.GetStartPositionLong());

            // 5
            if (mesg.GetEndPositionLat() != null)
                Log.Information("    End Position Lat: {0}", mesg.GetEndPositionLat());

            // 6
            if (mesg.GetEndPositionLong() != null)
                Log.Information("    End Position Long: {0}", mesg.GetEndPositionLong());

            // 7
            if (mesg.GetTotalElapsedTime() != null)
                Log.Information("    Total Elapsed Time: {0}", mesg.GetTotalElapsedTime());

            // 8
            if (mesg.GetTotalTimerTime() != null)
                Log.Information("    Total Timer Time: {0}", mesg.GetTotalTimerTime());

            // 9
            if (mesg.GetTotalDistance() != null)
                Log.Information("    Total Distance: {0}", mesg.GetTotalDistance());

            // 10
            if (mesg.GetTotalCycles() != null)
                Log.Information("    Total Cycles: {0}", mesg.GetTotalCycles());

            // 11
            if (mesg.GetTotalCalories() != null)
                Log.Information("    Total Calories: {0}", mesg.GetTotalCalories());

            // 12
            if (mesg.GetTotalFatCalories() != null)
                Log.Information("    Total Fat Calories: {0}", mesg.GetTotalFatCalories());

            // 13
            if (mesg.GetAvgSpeed() != null)
                Log.Information("    Avg Speed: {0}", mesg.GetAvgSpeed());

            // 14
            if (mesg.GetMaxSpeed() != null)
                Log.Information("    Max Speed: {0}", mesg.GetMaxSpeed());

            // 15
            if (mesg.GetAvgHeartRate() != null)
                Log.Information("    Avg HR: {0}", mesg.GetAvgHeartRate());

            // 16
            if (mesg.GetMaxHeartRate() != null)
                Log.Information("    Max HR: {0}", mesg.GetMaxHeartRate());

            // 17
            if (mesg.GetAvgCadence() != null)
                Log.Information("    Avg Cadence: {0}", mesg.GetAvgCadence());

            // 18
            if (mesg.GetMaxCadence() != null)
                Log.Information("    Max Cadence: {0}", mesg.GetMaxCadence());

            // 19
            if (mesg.GetAvgPower() != null)
                Log.Information("    Avg Power: {0}", mesg.GetAvgPower());

            // 20
            if (mesg.GetMaxPower() != null)
                Log.Information("    Max Power: {0}", mesg.GetMaxPower());

            // 21
            if (mesg.GetTotalAscent() != null)
                Log.Information("    Total Ascent: {0}", mesg.GetTotalAscent());

            // 22
            if (mesg.GetTotalDescent() != null)
                Log.Information("    Total Descent: {0}", mesg.GetTotalDescent());

            // 23
            if (mesg.GetIntensity() != null)
                Log.Information("    Intensity: {0}", mesg.GetIntensity());

            // 24
            if (mesg.GetLapTrigger() != null)
                Log.Information("    Lap Trigger: {0}", mesg.GetLapTrigger().ToString());

            // 25
            if (mesg.GetSport() != null)
                Log.Information("    Sport: {0}", mesg.GetSport().ToString());

            // 26
            if (mesg.GetEventGroup() != null)
                Log.Information("    Event Group: {0}", mesg.GetEventGroup());

            // 32
            if (mesg.GetNumLengths() != null)
                Log.Information("    Number of Lengths: {0}", mesg.GetNumLengths());

            // 33
            if (mesg.GetNormalizedPower() != null)
                Log.Information("    Normalized Power: {0}", mesg.GetNormalizedPower());

            // 34
            if (mesg.GetLeftRightBalance() != null)
                Log.Information("    Left/Right Balance: {0}", mesg.GetLeftRightBalance().ToString());

            // 35
            if (mesg.GetFirstLengthIndex() != null)
                Log.Information("    First Length Index: {0}", mesg.GetFirstLengthIndex());

            // 37
            if (mesg.GetAvgStrokeDistance() != null)
                Log.Information("    Avg Stroke Distance: {0}", mesg.GetAvgStrokeDistance());

            // 38
            if (mesg.GetSwimStroke() != null)
                Log.Information("    Swim Stroke: {0}", mesg.GetSwimStroke().ToString());

            // 39
            if (mesg.GetSubSport() != null)
                Log.Information("    SubSport: {0}", mesg.GetSubSport().ToString());

            // 40
            if (mesg.GetNumActiveLengths() != null)
                Log.Information("    Number of Active Lengths: {0}", mesg.GetNumActiveLengths());

            // 41
            if (mesg.GetTotalWork() != null)
                Log.Information("    Total Work: {0}", mesg.GetTotalWork());

            // 42
            if (mesg.GetAvgAltitude() != null)
                Log.Information("    Avg Altitude: {0}", mesg.GetAvgAltitude());

            // 43
            if (mesg.GetMaxAltitude() != null)
                Log.Information("    Max Altitude: {0}", mesg.GetMaxAltitude());

            // 44
            if (mesg.GetGpsAccuracy() != null)
                Log.Information("    GPS Accuracy: {0}", mesg.GetGpsAccuracy());

            // 45
            if (mesg.GetAvgGrade() != null)
                Log.Information("    Avg Grade: {0}", mesg.GetAvgGrade());

            // 46
            if (mesg.GetAvgPosGrade() != null)
                Log.Information("    Avg Positive Grade: {0}", mesg.GetAvgPosGrade());

            // 47
            if (mesg.GetAvgNegGrade() != null)
                Log.Information("    Avg Negative Grade: {0}", mesg.GetAvgNegGrade());

            // 48
            if (mesg.GetMaxPosGrade() != null)
                Log.Information("    Max Positive Grade: {0}", mesg.GetMaxPosGrade());

            // 49
            if (mesg.GetMaxNegGrade() != null)
                Log.Information("    Max Negative Grade: {0}", mesg.GetMaxNegGrade());

            // 50
            if (mesg.GetAvgTemperature() != null)
                Log.Information("    Avg Temperature: {0}", mesg.GetAvgTemperature());

            // 51
            if (mesg.GetMaxTemperature() != null)
                Log.Information("    Max Temperature: {0}", mesg.GetMaxTemperature());

            // 52
            if (mesg.GetTotalMovingTime() != null)
                Log.Information("    Total Moving Time: {0}", mesg.GetTotalMovingTime());

            // 53
            if (mesg.GetAvgPosVerticalSpeed() != null)
                Log.Information("    Avg Positive Vertical Speed: {0}", mesg.GetAvgPosVerticalSpeed());

            // 54
            if (mesg.GetAvgNegVerticalSpeed() != null)
                Log.Information("    Avg Negative Vertical Speed: {0}", mesg.GetAvgNegVerticalSpeed());

            // 55
            if (mesg.GetMaxPosVerticalSpeed() != null)
                Log.Information("    Max Positive Vertical Speed: {0}", mesg.GetMaxPosVerticalSpeed());

            // 56
            if (mesg.GetMaxNegVerticalSpeed() != null)
                Log.Information("    Max Negative Vertical Speed: {0}", mesg.GetMaxNegVerticalSpeed());

            // 57 (array)
            for (int i = 0; i < mesg.GetNumTimeInHrZone(); i++)
                Log.Information("    Time in HR Zone[{0}]: {1}", i, mesg.GetTimeInHrZone(i));

            // 58 (array)
            for (int i = 0; i < mesg.GetNumTimeInSpeedZone(); i++)
                Log.Information("    Time in Speed Zone[{0}]: {1}", i, mesg.GetTimeInSpeedZone(i));

            // 59 (array)
            for (int i = 0; i < mesg.GetNumTimeInCadenceZone(); i++)
                Log.Information("    Time in Cadence Zone[{0}]: {1}", i, mesg.GetTimeInCadenceZone(i));

            // 60 (array)
            for (int i = 0; i < mesg.GetNumTimeInPowerZone(); i++)
                Log.Information("    Time in Power Zone[{0}]: {1}", i, mesg.GetTimeInPowerZone(i));

            // 61
            if (mesg.GetRepetitionNum() != null)
                Log.Information("    Repetition Number: {0}", mesg.GetRepetitionNum());

            // 62
            if (mesg.GetMinAltitude() != null)
                Log.Information("    Min Altitude: {0}", mesg.GetMinAltitude());

            // 63
            if (mesg.GetMinHeartRate() != null)
                Log.Information("    Min HR: {0}", mesg.GetMinHeartRate());

            // 71
            if (mesg.GetWktStepIndex() != null)
                Log.Information("    Workout Step Index: {0}", mesg.GetWktStepIndex());

            // 74
            if (mesg.GetOpponentScore() != null)
                Log.Information("    Opponent Score: {0}", mesg.GetOpponentScore());

            // 75 (array)
            for (int i = 0; i < mesg.GetNumStrokeCount(); i++)
                Log.Information("    Stroke Count[{0}]: {1}", i, mesg.GetStrokeCount(i));

            // 76 (array)
            for (int i = 0; i < mesg.GetNumZoneCount(); i++)
                Log.Information("    Zone Count[{0}]: {1}", i, mesg.GetZoneCount(i));

            // 77
            if (mesg.GetAvgVerticalOscillation() != null)
                Log.Information("    Avg Vertical Oscillation: {0}", mesg.GetAvgVerticalOscillation());

            // 78
            if (mesg.GetAvgStanceTimePercent() != null)
                Log.Information("    Avg Stance Time Percent: {0}", mesg.GetAvgStanceTimePercent());

            // 79
            if (mesg.GetAvgStanceTime() != null)
                Log.Information("    Avg Stance Time: {0}", mesg.GetAvgStanceTime());

            // 80
            if (mesg.GetAvgFractionalCadence() != null)
                Log.Information("    Avg Fractional Cadence: {0}", mesg.GetAvgFractionalCadence());

            // 81
            if (mesg.GetMaxFractionalCadence() != null)
                Log.Information("    Max Fractional Cadence: {0}", mesg.GetMaxFractionalCadence());

            // 82
            if (mesg.GetTotalFractionalCycles() != null)
                Log.Information("    Total Fractional Cycles: {0}", mesg.GetTotalFractionalCycles());

            // 83
            if (mesg.GetPlayerScore() != null)
                Log.Information("    Player Score: {0}", mesg.GetPlayerScore());

            // 84 (array)
            for (int i = 0; i < mesg.GetNumAvgTotalHemoglobinConc(); i++)
                Log.Information("    Avg Total Hb Conc[{0}]: {1}", i, mesg.GetAvgTotalHemoglobinConc(i));

            // 85 (array)
            for (int i = 0; i < mesg.GetNumMinTotalHemoglobinConc(); i++)
                Log.Information("    Min Total Hb Conc[{0}]: {1}", i, mesg.GetMinTotalHemoglobinConc(i));

            // 86 (array)
            for (int i = 0; i < mesg.GetNumMaxTotalHemoglobinConc(); i++)
                Log.Information("    Max Total Hb Conc[{0}]: {1}", i, mesg.GetMaxTotalHemoglobinConc(i));

            // 87 (array)
            for (int i = 0; i < mesg.GetNumAvgSaturatedHemoglobinPercent(); i++)
                Log.Information("    Avg Hb Saturation[{0}]: {1}", i, mesg.GetAvgSaturatedHemoglobinPercent(i));

            // 88 (array)
            for (int i = 0; i < mesg.GetNumMinSaturatedHemoglobinPercent(); i++)
                Log.Information("    Min Hb Saturation[{0}]: {1}", i, mesg.GetMinSaturatedHemoglobinPercent(i));

            // 89 (array)
            for (int i = 0; i < mesg.GetNumMaxSaturatedHemoglobinPercent(); i++)
                Log.Information("    Max Hb Saturation[{0}]: {1}", i, mesg.GetMaxSaturatedHemoglobinPercent(i));

            // 91
            if (mesg.GetAvgLeftTorqueEffectiveness() != null)
                Log.Information("    Avg Left Torque Effectiveness: {0}", mesg.GetAvgLeftTorqueEffectiveness());

            // 92
            if (mesg.GetAvgRightTorqueEffectiveness() != null)
                Log.Information("    Avg Right Torque Effectiveness: {0}", mesg.GetAvgRightTorqueEffectiveness());

            // 93
            if (mesg.GetAvgLeftPedalSmoothness() != null)
                Log.Information("    Avg Left Pedal Smoothness: {0}", mesg.GetAvgLeftPedalSmoothness());

            // 94
            if (mesg.GetAvgRightPedalSmoothness() != null)
                Log.Information("    Avg Right Pedal Smoothness: {0}", mesg.GetAvgRightPedalSmoothness());

            // 95
            if (mesg.GetAvgCombinedPedalSmoothness() != null)
                Log.Information("    Avg Combined Pedal Smoothness: {0}", mesg.GetAvgCombinedPedalSmoothness());

            // 98
            if (mesg.GetTimeStanding() != null)
                Log.Information("    Time Standing: {0}", mesg.GetTimeStanding());

            // 99
            if (mesg.GetStandCount() != null)
                Log.Information("    Stand Count: {0}", mesg.GetStandCount());

            // 100
            if (mesg.GetAvgLeftPco() != null)
                Log.Information("    Avg Left PCO: {0}", mesg.GetAvgLeftPco());

            // 101
            if (mesg.GetAvgRightPco() != null)
                Log.Information("    Avg Right PCO: {0}", mesg.GetAvgRightPco());

            // 102 (array)
            for (int i = 0; i < mesg.GetNumAvgLeftPowerPhase(); i++)
                Log.Information("    Avg Left Power Phase[{0}]: {1}", i, mesg.GetAvgLeftPowerPhase(i));

            // 103 (array)
            for (int i = 0; i < mesg.GetNumAvgLeftPowerPhasePeak(); i++)
                Log.Information("    Avg Left Power Phase Peak[{0}]: {1}", i, mesg.GetAvgLeftPowerPhasePeak(i));

            // 104 (array)
            for (int i = 0; i < mesg.GetNumAvgRightPowerPhase(); i++)
                Log.Information("    Avg Right Power Phase[{0}]: {1}", i, mesg.GetAvgRightPowerPhase(i));

            // 105 (array)
            for (int i = 0; i < mesg.GetNumAvgRightPowerPhasePeak(); i++)
                Log.Information("    Avg Right Power Phase Peak[{0}]: {1}", i, mesg.GetAvgRightPowerPhasePeak(i));

            // 106 (array)
            for (int i = 0; i < mesg.GetNumAvgPowerPosition(); i++)
                Log.Information("    Avg Power Position[{0}]: {1}", i, mesg.GetAvgPowerPosition(i));

            // 107 (array)
            for (int i = 0; i < mesg.GetNumMaxPowerPosition(); i++)
                Log.Information("    Max Power Position[{0}]: {1}", i, mesg.GetMaxPowerPosition(i));

            // 108 (array)
            for (int i = 0; i < mesg.GetNumAvgCadencePosition(); i++)
                Log.Information("    Avg Cadence Position[{0}]: {1}", i, mesg.GetAvgCadencePosition(i));

            // 109 (array)
            for (int i = 0; i < mesg.GetNumMaxCadencePosition(); i++)
                Log.Information("    Max Cadence Position[{0}]: {1}", i, mesg.GetMaxCadencePosition(i));

            // 110
            if (mesg.GetEnhancedAvgSpeed() != null)
                Log.Information("    Enhanced Avg Speed: {0}", mesg.GetEnhancedAvgSpeed());

            // 111
            if (mesg.GetEnhancedMaxSpeed() != null)
                Log.Information("    Enhanced Max Speed: {0}", mesg.GetEnhancedMaxSpeed());

            // 112
            if (mesg.GetEnhancedAvgAltitude() != null)
                Log.Information("    Enhanced Avg Altitude: {0}", mesg.GetEnhancedAvgAltitude());

            // 113
            if (mesg.GetEnhancedMinAltitude() != null)
                Log.Information("    Enhanced Min Altitude: {0}", mesg.GetEnhancedMinAltitude());

            // 114
            if (mesg.GetEnhancedMaxAltitude() != null)
                Log.Information("    Enhanced Max Altitude: {0}", mesg.GetEnhancedMaxAltitude());

            // 115
            if (mesg.GetAvgLevMotorPower() != null)
                Log.Information("    Avg LEV Motor Power: {0}", mesg.GetAvgLevMotorPower());

            // 116
            if (mesg.GetMaxLevMotorPower() != null)
                Log.Information("    Max LEV Motor Power: {0}", mesg.GetMaxLevMotorPower());

            // 117
            if (mesg.GetLevBatteryConsumption() != null)
                Log.Information("    LEV Battery Consumption: {0}", mesg.GetLevBatteryConsumption());

            // 118
            if (mesg.GetAvgVerticalRatio() != null)
                Log.Information("    Avg Vertical Ratio: {0}", mesg.GetAvgVerticalRatio());

            // 119
            if (mesg.GetAvgStanceTimeBalance() != null)
                Log.Information("    Avg Stance Time Balance: {0}", mesg.GetAvgStanceTimeBalance());

            // 120
            if (mesg.GetAvgStepLength() != null)
                Log.Information("    Avg Step Length: {0}", mesg.GetAvgStepLength());

            // 121
            if (mesg.GetAvgVam() != null)
                Log.Information("    Avg VAM: {0}", mesg.GetAvgVam());

            // 122
            if (mesg.GetAvgDepth() != null)
                Log.Information("    Avg Depth: {0}", mesg.GetAvgDepth());

            // 123
            if (mesg.GetMaxDepth() != null)
                Log.Information("    Max Depth: {0}", mesg.GetMaxDepth());

            // 124
            if (mesg.GetMinTemperature() != null)
                Log.Information("    Min Temperature: {0}", mesg.GetMinTemperature());

            // 136
            if (mesg.GetEnhancedAvgRespirationRate() != null)
                Log.Information("    Enhanced Avg Respiration Rate: {0}", mesg.GetEnhancedAvgRespirationRate());

            // 137
            if (mesg.GetEnhancedMaxRespirationRate() != null)
                Log.Information("    Enhanced Max Respiration Rate: {0}", mesg.GetEnhancedMaxRespirationRate());

            // 147
            if (mesg.GetAvgRespirationRate() != null)
                Log.Information("    Avg Respiration Rate: {0}", mesg.GetAvgRespirationRate());

            // 148
            if (mesg.GetMaxRespirationRate() != null)
                Log.Information("    Max Respiration Rate: {0}", mesg.GetMaxRespirationRate());

            // 149
            if (mesg.GetTotalGrit() != null)
                Log.Information("    Total Grit: {0}", mesg.GetTotalGrit());

            // 150
            if (mesg.GetTotalFlow() != null)
                Log.Information("    Total Flow: {0}", mesg.GetTotalFlow());

            // 151
            if (mesg.GetJumpCount() != null)
                Log.Information("    Jump Count: {0}", mesg.GetJumpCount());

            // 153
            if (mesg.GetAvgGrit() != null)
                Log.Information("    Avg Grit: {0}", mesg.GetAvgGrit());

            // 154
            if (mesg.GetAvgFlow() != null)
                Log.Information("    Avg Flow: {0}", mesg.GetAvgFlow());

            // 156
            if (mesg.GetTotalFractionalAscent() != null)
                Log.Information("    Total Fractional Ascent: {0}", mesg.GetTotalFractionalAscent());

            // 157
            if (mesg.GetTotalFractionalDescent() != null)
                Log.Information("    Total Fractional Descent: {0}", mesg.GetTotalFractionalDescent());

            // 158
            if (mesg.GetAvgCoreTemperature() != null)
                Log.Information("    Avg Core Temperature: {0}", mesg.GetAvgCoreTemperature());

            // 159
            if (mesg.GetMinCoreTemperature() != null)
                Log.Information("    Min Core Temperature: {0}", mesg.GetMinCoreTemperature());

            // 160
            if (mesg.GetMaxCoreTemperature() != null)
                Log.Information("    Max Core Temperature: {0}", mesg.GetMaxCoreTemperature());

            // 253
            if (mesg.GetTimestamp() != null)
                Log.Information("    Timestamp: {0}", mesg.GetTimestamp().GetDateTime().ToString());

            // 254
            if (mesg.GetMessageIndex() != null)
                Log.Information("    Message Index: {0}", mesg.GetMessageIndex());

            Log.Information("");
        }

        public static void PrintUserProfileMesg(UserProfileMesg mesg)
        {
            Log.Information("User profile:");

            if (mesg.GetFriendlyNameAsString() != null)
            {
                Log.Information("\tFriendlyName: \"{0}\"", mesg.GetFriendlyNameAsString());
            }

            if (mesg.GetGender() != null)
            {
                Log.Information("\tGender: {0}", mesg.GetGender().ToString());
            }

            if (mesg.GetAge() != null)
            {
                Log.Information("\tAge: {0}", mesg.GetAge());

            }

            if (mesg.GetWeight() != null)
            {
                Log.Information("\tWeight:  {0}", mesg.GetWeight());

            }

            Log.Information("");
        }

        public static void PrintDeviceInfoMesg(DeviceInfoMesg mesg)
        {
            Log.Information("Device info:");
            if (mesg.GetTimestamp() != null)
            {
                Log.Information("\tTimestamp: {0}", mesg.GetTimestamp().ToString());
            }

            if (mesg.GetBatteryStatus() != null)
            {
                Log.Information("\tBattery Status: ");

                switch (mesg.GetBatteryStatus())
                {
                    case BatteryStatus.Critical:
                        Log.Information("Critical");
                        break;
                    case BatteryStatus.Good:
                        Log.Information("Good");
                        break;
                    case BatteryStatus.Low:
                        Log.Information("Low");
                        break;
                    case BatteryStatus.New:
                        Log.Information("New");
                        break;
                    case BatteryStatus.Ok:
                        Log.Information("OK");
                        break;
                    default:
                        Log.Information("Invalid");
                        break;
                }
            }

            Log.Information("");
        }

        public static void PrintMonitoringMesg(MonitoringMesg mesg)
        {
            Log.Information("Monitoring:");
            if (mesg.GetTimestamp() != null)
            {
                Log.Information("\tTimestamp: {0}", mesg.GetTimestamp().ToString());
            }

            if (mesg.GetActivityType() != null)
            {
                Log.Information("\tActivityType: {0}", mesg.GetActivityType());
                switch (mesg.GetActivityType()) // Cycles is a dynamic field
                {
                    case ActivityType.Walking:
                    case ActivityType.Running:
                        Log.Information("\tSteps: {0}", mesg.GetSteps());
                        break;
                    case ActivityType.Cycling:
                    case ActivityType.Swimming:
                        Log.Information("\tStrokes: {0}", mesg.GetStrokes());
                        break;
                    default:
                        Log.Information("\tCycles: {0}", mesg.GetCycles());
                        break;
                }
            }

            Log.Information("");
        }

        public static void PrintRecordMesg(RecordMesg mesg)
        {
            Log.Information("Record:");

            //PrintFieldWithOverrides(mesg, RecordMesg.FieldDefNum.HeartRate);
            //PrintFieldWithOverrides(mesg, RecordMesg.FieldDefNum.Cadence);
            //PrintFieldWithOverrides(mesg, RecordMesg.FieldDefNum.Speed);
            //PrintFieldWithOverrides(mesg, RecordMesg.FieldDefNum.Distance);
            //PrintFieldWithOverrides(mesg, RecordMesg.FieldDefNum.EnhancedAltitude);
            //
            //PrintDeveloperFields(mesg);

            // 0
            if (mesg.GetPositionLat() != null)
                Log.Information("    Position Lat: {0}", mesg.GetPositionLat());

            // 1
            if (mesg.GetPositionLong() != null)
                Log.Information("    Position Long: {0}", mesg.GetPositionLong());

            // 2
            if (mesg.GetAltitude() != null)
                Log.Information("    Altitude: {0}", mesg.GetAltitude());

            // 3
            if (mesg.GetHeartRate() != null)
                Log.Information("    Heart Rate: {0}", mesg.GetHeartRate());

            // 4
            if (mesg.GetCadence() != null)
                Log.Information("    Cadence: {0}", mesg.GetCadence());

            // 5
            if (mesg.GetDistance() != null)
                Log.Information("    Distance: {0}", mesg.GetDistance());

            // 6
            if (mesg.GetSpeed() != null)
                Log.Information("    Speed: {0}", mesg.GetSpeed());

            // 7
            if (mesg.GetPower() != null)
                Log.Information("    Power: {0}", mesg.GetPower());

            // 8
            for (int i = 0; i < mesg.GetNumCompressedSpeedDistance(); i++)
                Log.Information("    Compressed Speed/Distance[{0}]: {1}", i, mesg.GetCompressedSpeedDistance(i));

            // 9
            if (mesg.GetGrade() != null)
                Log.Information("    Grade: {0}", mesg.GetGrade());

            // 10
            if (mesg.GetResistance() != null)
                Log.Information("    Resistance: {0}", mesg.GetResistance());

            // 11
            if (mesg.GetTimeFromCourse() != null)
                Log.Information("    Time From Course: {0}", mesg.GetTimeFromCourse());

            // 12
            if (mesg.GetCycleLength() != null)
                Log.Information("    Cycle Length: {0}", mesg.GetCycleLength());

            // 13
            if (mesg.GetTemperature() != null)
                Log.Information("    Temperature: {0}", mesg.GetTemperature());

            // 17 (array)
            for (int i = 0; i < mesg.GetNumSpeed1s(); i++)
                Log.Information("    Speed 1s[{0}]: {1}", i, mesg.GetSpeed1s(i));

            // 18
            if (mesg.GetCycles() != null)
                Log.Information("    Cycles: {0}", mesg.GetCycles());

            // 19
            if (mesg.GetTotalCycles() != null)
                Log.Information("    Total Cycles: {0}", mesg.GetTotalCycles());

            // 28
            if (mesg.GetCompressedAccumulatedPower() != null)
                Log.Information("    Compressed Accumulated Power: {0}", mesg.GetCompressedAccumulatedPower());

            // 29
            if (mesg.GetAccumulatedPower() != null)
                Log.Information("    Accumulated Power: {0}", mesg.GetAccumulatedPower());

            // 30
            if (mesg.GetLeftRightBalance() != null)
                Log.Information("    Left/Right Balance: {0}", mesg.GetLeftRightBalance().ToString());

            // 31
            if (mesg.GetGpsAccuracy() != null)
                Log.Information("    GPS Accuracy: {0}", mesg.GetGpsAccuracy());

            // 32
            if (mesg.GetVerticalSpeed() != null)
                Log.Information("    Vertical Speed: {0}", mesg.GetVerticalSpeed());

            // 33
            if (mesg.GetCalories() != null)
                Log.Information("    Calories: {0}", mesg.GetCalories());

            // 39
            if (mesg.GetVerticalOscillation() != null)
                Log.Information("    Vertical Oscillation: {0}", mesg.GetVerticalOscillation());

            // 40
            if (mesg.GetStanceTimePercent() != null)
                Log.Information("    Stance Time Percent: {0}", mesg.GetStanceTimePercent());

            // 41
            if (mesg.GetStanceTime() != null)
                Log.Information("    Stance Time: {0}", mesg.GetStanceTime());

            // 42
            if (mesg.GetActivityType() != null)
                Log.Information("    Activity Type: {0}", mesg.GetActivityType().ToString());

            // 43
            if (mesg.GetLeftTorqueEffectiveness() != null)
                Log.Information("    Left Torque Effectiveness: {0}", mesg.GetLeftTorqueEffectiveness());

            // 44
            if (mesg.GetRightTorqueEffectiveness() != null)
                Log.Information("    Right Torque Effectiveness: {0}", mesg.GetRightTorqueEffectiveness());

            // 45
            if (mesg.GetLeftPedalSmoothness() != null)
                Log.Information("    Left Pedal Smoothness: {0}", mesg.GetLeftPedalSmoothness());

            // 46
            if (mesg.GetRightPedalSmoothness() != null)
                Log.Information("    Right Pedal Smoothness: {0}", mesg.GetRightPedalSmoothness());

            // 47
            if (mesg.GetCombinedPedalSmoothness() != null)
                Log.Information("    Combined Pedal Smoothness: {0}", mesg.GetCombinedPedalSmoothness());

            // 48
            if (mesg.GetTime128() != null)
                Log.Information("    Time128: {0}", mesg.GetTime128());

            // 49
            if (mesg.GetStrokeType() != null)
                Log.Information("    Stroke Type: {0}", mesg.GetStrokeType().ToString());

            // 50
            if (mesg.GetZone() != null)
                Log.Information("    Zone: {0}", mesg.GetZone());

            // 51
            if (mesg.GetBallSpeed() != null)
                Log.Information("    Ball Speed: {0}", mesg.GetBallSpeed());

            // 52
            if (mesg.GetCadence256() != null)
                Log.Information("    Cadence256: {0}", mesg.GetCadence256());

            // 53
            if (mesg.GetFractionalCadence() != null)
                Log.Information("    Fractional Cadence: {0}", mesg.GetFractionalCadence());

            // 54 (array)
            if (mesg.GetTotalHemoglobinConc() != null)
                Log.Information("    Total Hb Conc: {0}", mesg.GetTotalHemoglobinConc());

            // 55 (array)
            if (mesg.GetTotalHemoglobinConcMin() != null)
                Log.Information("    Total Hb Conc Min: {0}", mesg.GetTotalHemoglobinConcMin());

            // 56 (array)
            if (mesg.GetTotalHemoglobinConcMax() != null)
                Log.Information("    Total Hb Conc Max: {0}", mesg.GetTotalHemoglobinConcMax());

            // 57 (array)
            if (mesg.GetSaturatedHemoglobinPercent() != null)
                Log.Information("    Hb Saturation: {0}", mesg.GetSaturatedHemoglobinPercent());

            // 58 (array)
            if (mesg.GetSaturatedHemoglobinPercentMin() != null)
                Log.Information("    Hb Saturation Min: {0}", mesg.GetSaturatedHemoglobinPercentMin());

            // 59 (array)
            if (mesg.GetSaturatedHemoglobinPercentMax() != null)
                Log.Information("    Hb Saturation Max: {0}", mesg.GetSaturatedHemoglobinPercentMax());

            // 62
            if (mesg.GetDeviceIndex() != null)
                Log.Information("    Device Index: {0}", mesg.GetDeviceIndex());

            // 67
            if (mesg.GetLeftPco() != null)
                Log.Information("    Left PCO: {0}", mesg.GetLeftPco());

            // 68
            if (mesg.GetRightPco() != null)
                Log.Information("    Right PCO: {0}", mesg.GetRightPco());

            // 69 (array)
            for (int i = 0; i < mesg.GetNumLeftPowerPhase(); i++)
                Log.Information("    Left Power Phase[{0}]: {1}", i, mesg.GetLeftPowerPhase(i));

            // 70 (array)
            for (int i = 0; i < mesg.GetNumLeftPowerPhasePeak(); i++)
                Log.Information("    Left Power Phase Peak[{0}]: {1}", i, mesg.GetLeftPowerPhasePeak(i));

            // 71 (array)
            for (int i = 0; i < mesg.GetNumRightPowerPhase(); i++)
                Log.Information("    Right Power Phase[{0}]: {1}", i, mesg.GetRightPowerPhase(i));

            // 72 (array)
            for (int i = 0; i < mesg.GetNumRightPowerPhasePeak(); i++)
                Log.Information("    Right Power Phase Peak[{0}]: {1}", i, mesg.GetRightPowerPhasePeak(i));

            // 73
            if (mesg.GetEnhancedSpeed() != null)
                Log.Information("    Enhanced Speed: {0}", mesg.GetEnhancedSpeed());

            // 78
            if (mesg.GetEnhancedAltitude() != null)
                Log.Information("    Enhanced Altitude: {0}", mesg.GetEnhancedAltitude());

            // 81
            if (mesg.GetBatterySoc() != null)
                Log.Information("    Battery SOC: {0}", mesg.GetBatterySoc());

            // 82
            if (mesg.GetMotorPower() != null)
                Log.Information("    Motor Power: {0}", mesg.GetMotorPower());

            // 83
            if (mesg.GetVerticalRatio() != null)
                Log.Information("    Vertical Ratio: {0}", mesg.GetVerticalRatio());

            // 84
            if (mesg.GetStanceTimeBalance() != null)
                Log.Information("    Stance Time Balance: {0}", mesg.GetStanceTimeBalance());

            // 85
            if (mesg.GetStepLength() != null)
                Log.Information("    Step Length: {0}", mesg.GetStepLength());

            // 87
            if (mesg.GetCycleLength16() != null)
                Log.Information("    Cycle Length 16: {0}", mesg.GetCycleLength16());

            // 91
            if (mesg.GetAbsolutePressure() != null)
                Log.Information("    Absolute Pressure: {0}", mesg.GetAbsolutePressure());

            // 92
            if (mesg.GetDepth() != null)
                Log.Information("    Depth: {0}", mesg.GetDepth());

            // 93
            if (mesg.GetNextStopDepth() != null)
                Log.Information("    Next Stop Depth: {0}", mesg.GetNextStopDepth());

            // 94
            if (mesg.GetNextStopTime() != null)
                Log.Information("    Next Stop Time: {0}", mesg.GetNextStopTime());

            // 95
            if (mesg.GetTimeToSurface() != null)
                Log.Information("    Time To Surface: {0}", mesg.GetTimeToSurface());

            // 96
            if (mesg.GetNdlTime() != null)
                Log.Information("    NDL Time: {0}", mesg.GetNdlTime());

            // 97
            if (mesg.GetCnsLoad() != null)
                Log.Information("    CNS Load: {0}", mesg.GetCnsLoad());

            // 98
            if (mesg.GetN2Load() != null)
                Log.Information("    N2 Load: {0}", mesg.GetN2Load());

            // 99
            if (mesg.GetRespirationRate() != null)
                Log.Information("    Respiration Rate: {0}", mesg.GetRespirationRate());

            // 108
            if (mesg.GetEnhancedRespirationRate() != null)
                Log.Information("    Enhanced Respiration Rate: {0}", mesg.GetEnhancedRespirationRate());

            // 114
            if (mesg.GetGrit() != null)
                Log.Information("    Grit: {0}", mesg.GetGrit());

            // 115
            if (mesg.GetFlow() != null)
                Log.Information("    Flow: {0}", mesg.GetFlow());

            // 116
            if (mesg.GetCurrentStress() != null)
                Log.Information("    Current Stress: {0}", mesg.GetCurrentStress());

            // 117
            if (mesg.GetEbikeTravelRange() != null)
                Log.Information("    Ebike Travel Range: {0}", mesg.GetEbikeTravelRange());

            // 118
            if (mesg.GetEbikeBatteryLevel() != null)
                Log.Information("    Ebike Battery Level: {0}", mesg.GetEbikeBatteryLevel());

            // 119
            if (mesg.GetEbikeAssistMode() != null)
                Log.Information("    Ebike Assist Mode: {0}", mesg.GetEbikeAssistMode().ToString());

            // 120
            if (mesg.GetEbikeAssistLevelPercent() != null)
                Log.Information("    Ebike Assist Level Percent: {0}", mesg.GetEbikeAssistLevelPercent());

            // 123
            if (mesg.GetAirTimeRemaining() != null)
                Log.Information("    Air Time Remaining: {0}", mesg.GetAirTimeRemaining());

            // 124
            if (mesg.GetPressureSac() != null)
                Log.Information("    Pressure SAC: {0}", mesg.GetPressureSac());

            // 125
            if (mesg.GetVolumeSac() != null)
                Log.Information("    Volume SAC: {0}", mesg.GetVolumeSac());

            // 126
            if (mesg.GetRmv() != null)
                Log.Information("    RMV: {0}", mesg.GetRmv());

            // 127
            if (mesg.GetAscentRate() != null)
                Log.Information("    Ascent Rate: {0}", mesg.GetAscentRate());

            // 129
            if (mesg.GetPo2() != null)
                Log.Information("    PO2: {0}", mesg.GetPo2());

            // 139
            if (mesg.GetCoreTemperature() != null)
                Log.Information("    Core Temperature: {0}", mesg.GetCoreTemperature());

            // 253
            if (mesg.GetTimestamp() != null)
            {
                Log.Information("    Timestamp: {0}", mesg.GetTimestamp().GetDateTime().ToString());
            }

            Log.Information("");
        }

        public static void PrintDeveloperFields(Mesg mesg)
        {
            foreach (var devField in mesg.DeveloperFields)
            {
                if (devField.GetNumValues() <= 0)
                {
                    continue;
                }

                StringBuilder sb = new StringBuilder();
                if (devField.IsDefined)
                {
                    sb.AppendFormat("\t{0}", devField.Name);

                    if (devField.Units != null)
                    {
                        sb.AppendFormat(" [{0}]", devField.Units);
                    }
                    sb.Append(": ");
                }
                else
                {
                    sb.Append("\tUndefined Field: ");
                }

                sb.AppendFormat("{0}", devField.GetValue(0));
                for (int i = 1; i < devField.GetNumValues(); i++)
                {
                    sb.AppendFormat(",{0}", devField.GetValue(i));
                }

                Log.Information(sb.ToString());
                sb.Clear();
            }
        }

        public static void PrintFieldWithOverrides(Mesg mesg, byte fieldNumber)
        {
            Field profileField = Profile.GetField(mesg.Num, fieldNumber);
            bool nameWritten = false;

            if (null == profileField)
            {
                return;
            }

            IEnumerable<FieldBase> fields = mesg.GetOverrideField(fieldNumber);

            foreach (FieldBase field in fields)
            {
                if (!nameWritten)
                {
                    Log.Information("   {0}", profileField.GetName());
                    nameWritten = true;
                }

                if (field is Field)
                {
                    Log.Information("      native: {0}", field.GetValue());
                }
                else
                {
                    Log.Information("      override: {0}", field.GetValue());
                }
            }
        }

        public static SysDateTime GetFitFileTimeCreated(FileInfo fi)
        {
            FileStream fitSource = null;
            try
            {
                // Attempt to open .FIT file
                fitSource = new FileStream(fi.FullName, FileMode.Open);

                Decode decodeDemo = new Decode();

                // Use a FitListener to capture all decoded messages in a FitMessages object
                FitListener fitListener = new FitListener();
                decodeDemo.MesgEvent += fitListener.OnMesg;

                decodeDemo.Read(fitSource);

                FitMessages fitMessages = fitListener.FitMessages;
                return fitMessages.FileIdMesgs[0].GetTimeCreated().GetDateTime();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
            finally
            {
                fitSource.Close();
            }

            return new SysDateTime();
        }

        public static void FixLocalActivity(FileInfo file, int steps, CancellationTokenSource cancellation_source)
        {
            file.Refresh();

            SysDateTime tc = new SysDateTime();

            {//Fix the Fit file
                try
                {
                    cancellation_source.Token.ThrowIfCancellationRequested();
                    // Attempt to open .FIT file
                    Log.Information($"Decoding {0}", file.Name);
                    FitMessages fitMessages = Decode(file);

                    if (tc.Year < 2000)
                    {
                        tc = fitMessages.FileIdMesgs[0].GetTimeCreated().GetDateTime();
                    }
                    cancellation_source.Token.ThrowIfCancellationRequested();
                    Log.Information($"File Activity created {tc.ToLocalTime().ToString()}");

                    if (!DoesSessionContainSteps(ref fitMessages))
                    {
                        //Rename original file so we can use the correct name for the modified
                        Log.Information($"Adding steps");
                        double strides_f = Math.Floor((double)((float)steps / 2.0f));
                        uint strides = Convert.ToUInt32(strides_f);
                        fitMessages.SessionMesgs[0].SetTotalStrides(strides);
                        fitMessages.SessionMesgs[0].SetTotalCycles(strides);

                        fitMessages.SessionMesgs[0].SetEvent(Event.Session);
                        fitMessages.SessionMesgs[0].SetEventType(EventType.Stop);

                        //This is needed because Strava seems to have changed how it parses fit files
                        //unless it was Xiaomi who changed the export. The pain point seems to be multiple
                        //RecordMsg definitions. The first 5-6 data messages come with 5 fields, missing lat/long.
                        //Then a new RecordMsg comes which adds them. Likely Strava doesn't expect that.
                        //So the solution is to add lat/long to those that don't have them. Encode will create
                        //only one definition message.
                        MakeRecordsUniform(fitMessages);

                        CleanupFit(fitMessages);

                        cancellation_source.Token.ThrowIfCancellationRequested();
                        Log.Information($"Overwriting file");
                        Encode(file, ref fitMessages);
                        Log.Information($"Finished overwriting file");

                        cancellation_source.Token.ThrowIfCancellationRequested();
                        bool is_output_valid = CheckIntegrity(file);
                        is_output_valid &= CheckDecodeability(file);
                        if (!is_output_valid)
                        {
                            Log.Error($"File {file.Name} is not valid.");
                            cancellation_source.Cancel();
                        }
                    }
                    else
                    {
                        Log.Information("File already has cycles/strides.");
                        bool is_output_valid = CheckIntegrity(file);
                        if (!is_output_valid)
                        {
                            Log.Error($"File {file.Name} is not valid.");
                            cancellation_source.Cancel();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (FitException ex)
                {
                    Log.Error($"A FitException occurred when trying to decode the FIT file. Message: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Log.Error($"Exception occurred when trying to decode the FIT file. Message: " + ex.Message);
                }
            }
        }

        private static bool CheckDecodeability(FileInfo file)
        {
            try
            {
                FitMessages fitMessages = Decode(file);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void MakeRecordsUniform(FitMessages msgs)
        {
            RecordMesg first_full_rec_mesg = null;
            int first_rec_msg_field_count = msgs.RecordMesgs[0].GetNumFields();
            for (int i = 1; i < msgs.RecordMesgs.Count; i++)
            {
                if (msgs.RecordMesgs[i].GetNumFields() > first_rec_msg_field_count)
                {
                    first_full_rec_mesg = msgs.RecordMesgs[i];
                    break;
                }
            }

            if (first_full_rec_mesg != null) 
            {
                for (int i = 0; i < msgs.RecordMesgs.Count; i++)
                {
                    if (msgs.RecordMesgs[i].GetNumFields() < first_full_rec_mesg.GetNumFields())
                    {
                        msgs.RecordMesgs[i].SetPositionLat(0);
                        msgs.RecordMesgs[i].SetPositionLong(0);
                    }
                }
            }
        }

        public static void CleanupFit(FitMessages msgs)
        {
            try
            {
                foreach (FileIdMesg mesg in msgs.FileIdMesgs)
                {
                    mesg.RemoveExpandedFields();
                }
                foreach (ActivityMesg mesg in msgs.ActivityMesgs)
                {
                    mesg.RemoveExpandedFields();
                }
                foreach (SessionMesg mesg in msgs.SessionMesgs)
                {
                    mesg.RemoveExpandedFields();
                }
                foreach (LapMesg mesg in msgs.LapMesgs)
                {
                    mesg.RemoveExpandedFields();
                }
                foreach (RecordMesg mesg in msgs.RecordMesgs)
                {
                    mesg.RemoveExpandedFields();
                }
            }
            catch (FitException ex)
            {
                Log.Error("A FitException occurred when trying to cleanup expanded field artifacts. Message: " + ex.Message);
            }
            catch (Exception ex)
            {
                Log.Error("Exception occurred when trying to to cleanup expanded field artifacts. Message: " + ex.Message);
            }
        }

        public static void DumpFit(FileInfo fit)
        {
            try
            {
                Log.Information("Decoding...");
                FitMessages fitMessages = Decode(fit);

                Log.Information("Decoded FIT file {0}", fit.Name);

                SysDateTime timeCreated = new SysDateTime();

                foreach (FileIdMesg mesg in fitMessages.FileIdMesgs)
                {
                    timeCreated = mesg.GetTimeCreated().GetDateTime();
                    PrintFileIdMesg(mesg);
                }
                foreach (ActivityMesg mesg in fitMessages.ActivityMesgs)
                {
                    PrintActivityMesg(mesg, timeCreated);
                }
                foreach (SessionMesg mesg in fitMessages.SessionMesgs)
                {
                    PrintSessionMesg(mesg);
                }
                foreach (LapMesg mesg in fitMessages.LapMesgs)
                {
                    PrintLapMesg(mesg);
                }
                for(int i=0;i<10;i++)
                {
                    RecordMesg mesg = fitMessages.RecordMesgs[i];
                    PrintRecordMesg(mesg);
                }
                Log.Information("");
                Log.Information("...");
                Log.Information("");
                for (int i = fitMessages.RecordMesgs.Count - 10; i < fitMessages.RecordMesgs.Count; i++)
                {
                    RecordMesg mesg = fitMessages.RecordMesgs[i];
                    PrintRecordMesg(mesg);
                }
            }
            catch (FitException ex)
            {
                Log.Error("A FitException occurred when trying to decode the FIT file. Message: " + ex.Message);
            }
            catch (Exception ex)
            {
                Log.Error("Exception occurred when trying to decode the FIT file. Message: " + ex.Message);
            }
        }
    }
}
