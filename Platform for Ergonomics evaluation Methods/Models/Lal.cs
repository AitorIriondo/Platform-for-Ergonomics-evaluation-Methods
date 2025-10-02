using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace PEM.Models
{
    public class Lal
    {
        public Dictionary<string, Dictionary<string, double>> Compute(ManikinBase manikin)
        {
            if (manikin == null)
                throw new ArgumentNullException(nameof(manikin));

            if (manikin.postureTimeSteps == null || manikin.postureTimeSteps.Count == 0)
                throw new InvalidOperationException("No time steps found in manikin data.");

            // Series
            var headFlex = new List<double>();
            var uaElevL = new List<double>();
            var uaElevR = new List<double>();
            var uaVelL = new List<double>();
            var uaVelR = new List<double>();
            var wristVelL = new List<double>();
            var wristVelR = new List<double>();

            // Previous samples for velocity calculation
            double? prevUaL = null, prevUaR = null, prevWrL = null, prevWrR = null;
            float? prevTime = null;

            foreach (var t in manikin.postureTimeSteps)
            {
                manikin.SetTime(t);

                // Spine vector (C7T1 relative to L5S1)
                if (!manikin.TryGetJointPosition(JointID.L5S1, out var l5s1)) continue;
                if (!manikin.TryGetJointPosition(JointID.C7T1, out var c7)) continue;
                var trunk = SafeDir(c7 - l5s1);
                var vertical = new Vector3(0, 0, 1);

                // Head flexion proxy (trunk vs vertical)
                headFlex.Add(AngleDeg(trunk, vertical));

                // Upper arm elevation left
                if (manikin.TryGetJointPosition(JointID.LeftShoulder, out var lSh) &&
                    manikin.TryGetJointPosition(JointID.LeftElbow, out var lEl))
                {
                    var uvecL = SafeDir(lEl - lSh);
                    var elevL = AngleDeg(uvecL, trunk);
                    uaElevL.Add(elevL);

                    if (prevUaL.HasValue && prevTime.HasValue)
                        uaVelL.Add(Math.Abs(elevL - prevUaL.Value) / (t - prevTime.Value));

                    prevUaL = elevL;
                }

                // Upper arm elevation right
                if (manikin.TryGetJointPosition(JointID.RightShoulder, out var rSh) &&
                    manikin.TryGetJointPosition(JointID.RightElbow, out var rEl))
                {
                    var uvecR = SafeDir(rEl - rSh);
                    var elevR = AngleDeg(uvecR, trunk);
                    uaElevR.Add(elevR);

                    if (prevUaR.HasValue && prevTime.HasValue)
                        uaVelR.Add(Math.Abs(elevR - prevUaR.Value) / (t - prevTime.Value));

                    prevUaR = elevR;
                }

                // Wrist angular velocity left
                if (manikin.TryGetJointPosition(JointID.LeftElbow, out lEl) &&
                    manikin.TryGetJointPosition(JointID.LeftWrist, out var lWr))
                {
                    var foreL = lEl - lWr;
                    var knuckleL = lWr + 0.36f * SafeDir(foreL) * foreL.Length();
                    var handDirL = SafeDir(knuckleL - lWr);
                    var foreDirL = SafeDir(foreL);
                    var wristAngL = AngleDeg(foreDirL, handDirL);

                    if (prevWrL.HasValue && prevTime.HasValue)
                        wristVelL.Add(Math.Abs(wristAngL - prevWrL.Value) / (t - prevTime.Value));

                    prevWrL = wristAngL;
                }

                // Wrist angular velocity right
                if (manikin.TryGetJointPosition(JointID.RightElbow, out rEl) &&
                    manikin.TryGetJointPosition(JointID.RightWrist, out var rWr))
                {
                    var foreR = rEl - rWr;
                    var knuckleR = rWr + 0.36f * SafeDir(foreR) * foreR.Length();
                    var handDirR = SafeDir(knuckleR - rWr);
                    var foreDirR = SafeDir(foreR);
                    var wristAngR = AngleDeg(foreDirR, handDirR);

                    if (prevWrR.HasValue && prevTime.HasValue)
                        wristVelR.Add(Math.Abs(wristAngR - prevWrR.Value) / (t - prevTime.Value));

                    prevWrR = wristAngR;
                }

                prevTime = t;
            }

            var outDict = new Dictionary<string, Dictionary<string, double>>();
            AddPercentiles(outDict, "HeadFlexion", headFlex);
            AddPercentiles(outDict, "UpperArmAngleLeft", uaElevL);
            AddPercentiles(outDict, "UpperArmAngleRight", uaElevR);
            AddPercentiles(outDict, "UpperArmVelocityLeft", uaVelL);
            AddPercentiles(outDict, "UpperArmVelocityRight", uaVelR);
            AddPercentiles(outDict, "WristVelocityLeft", wristVelL);
            AddPercentiles(outDict, "WristVelocityRight", wristVelR);

            return outDict;
        }

        private static Vector3 SafeDir(Vector3 v)
        {
            var len = v.Length();
            if (len <= 1e-6f) return Vector3.UnitZ;
            return v / len;
        }

        private static double AngleDeg(Vector3 a, Vector3 b)
        {
            var dot = Math.Clamp(Vector3.Dot(a, b), -1f, 1f);
            return Math.Acos(dot) * (180.0 / Math.PI);
        }

        private static void AddPercentiles(Dictionary<string, Dictionary<string, double>> dst,
                                           string key, List<double> series)
        {
            if (series == null || series.Count == 0)
            {
                dst[key] = new Dictionary<string, double> { { "p10", 0 }, { "p50", 0 }, { "p90", 0 } };
                return;
            }
            series.Sort();
            dst[key] = new Dictionary<string, double>
            {
                { "p10", GetPercentile(series, 10) },
                { "p50", GetPercentile(series, 50) },
                { "p90", GetPercentile(series, 90) },
            };
        }

        private static double GetPercentile(List<double> sortedValues, double percentile)
        {
            if (sortedValues.Count == 1) return sortedValues[0];

            double pos = (sortedValues.Count - 1) * percentile / 100.0;
            int lo = (int)Math.Floor(pos);
            int hi = (int)Math.Ceiling(pos);
            if (hi == lo) return sortedValues[lo];
            double f = pos - lo;
            return sortedValues[lo] * (1 - f) + sortedValues[hi] * f;
        }
    }
}
