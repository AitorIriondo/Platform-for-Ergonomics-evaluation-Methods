using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace PEM.Models
{
    /// <summary>
    /// LAL (Lund Action Levels) helper that computes 10th/50th/90th percentiles
    /// for fixed metrics derived from the loaded ManikinBase timeline:
    /// - HeadFlexion (approximated using trunk C7–L5S1 line)
    /// - UpperArmElevation (left/right, relative to trunk C7–L5S1)
    /// - UpperArmVelocity (left/right, deg/s from UpperArmElevation)
    /// - WristVelocity (left/right, deg/s from angle between forearm and hand)
    ///
    /// NOTE: Single class by design (no extra DTOs).
    /// </summary>
    public class Lal
    {
        public Dictionary<string, Dictionary<string, double>> Compute(ManikinBase manikin, float dtSeconds = 0.1f)
        {
            if (manikin == null)
                throw new ArgumentNullException(nameof(manikin));

            var duration = manikin.GetTimelineDuration();
            if (duration <= 0) throw new InvalidOperationException("Manikin timeline has zero duration.");

            // Series
            var headFlex = new List<double>(); // deg
            var uaElevL = new List<double>();
            var uaElevR = new List<double>();
            var uaVelL = new List<double>(); // deg/s
            var uaVelR = new List<double>();
            var wristVelL = new List<double>();
            var wristVelR = new List<double>();

            // Previous samples for velocities
            double? prevUaL = null, prevUaR = null, prevWrL = null, prevWrR = null;

            for (float t = 0; t < duration; t += dtSeconds)
            {
                manikin.SetTime(t);

                // Trunk (C7 relative to L5S1)
                if (!manikin.TryGetJointPosition(JointID.L5S1, out var l5s1)) continue;
                if (!manikin.TryGetJointPosition(JointID.C7T1, out var c7)) continue;
                var trunk = SafeDir(c7 - l5s1);
                var vertical = new Vector3(0, 0, 1);

                // HeadFlex proxy (trunk flexion vs global vertical)
                headFlex.Add(AngleDeg(trunk, vertical));

                // Upper arm elevation: angle between (Shoulder→Elbow) and trunk
                if (manikin.TryGetJointPosition(JointID.LeftShoulder, out var lSh) &&
                    manikin.TryGetJointPosition(JointID.LeftElbow, out var lEl))
                {
                    var uvecL = SafeDir(lEl - lSh);
                    var elevL = AngleDeg(uvecL, trunk);
                    uaElevL.Add(elevL);
                    if (prevUaL.HasValue) uaVelL.Add(Math.Abs(elevL - prevUaL.Value) / dtSeconds);
                    prevUaL = elevL;
                }

                if (manikin.TryGetJointPosition(JointID.RightShoulder, out var rSh) &&
                    manikin.TryGetJointPosition(JointID.RightElbow, out var rEl))
                {
                    var uvecR = SafeDir(rEl - rSh);
                    var elevR = AngleDeg(uvecR, trunk);
                    uaElevR.Add(elevR);
                    if (prevUaR.HasValue) uaVelR.Add(Math.Abs(elevR - prevUaR.Value) / dtSeconds);
                    prevUaR = elevR;
                }

                // Wrist angular velocity: angle between forearm (Elbow→Wrist) and "hand" direction.
                // Hand direction ≈ wrist→knuckle at 36% of forearm length (same approximation used by AFF).
                if (manikin.TryGetJointPosition(JointID.LeftElbow, out lEl) &&
                    manikin.TryGetJointPosition(JointID.LeftWrist, out var lWr))
                {
                    var foreL = lEl - lWr;
                    var knuckleL = lWr + 0.36f * SafeDir(foreL) * foreL.Length();
                    var handDirL = SafeDir(knuckleL - lWr);
                    var foreDirL = SafeDir(foreL);
                    var wristAngL = AngleDeg(foreDirL, handDirL);
                    if (prevWrL.HasValue) wristVelL.Add(Math.Abs(wristAngL - prevWrL.Value) / dtSeconds);
                    prevWrL = wristAngL;
                }

                if (manikin.TryGetJointPosition(JointID.RightElbow, out rEl) &&
                    manikin.TryGetJointPosition(JointID.RightWrist, out var rWr))
                {
                    var foreR = rEl - rWr;
                    var knuckleR = rWr + 0.36f * SafeDir(foreR) * foreR.Length();
                    var handDirR = SafeDir(knuckleR - rWr);
                    var foreDirR = SafeDir(foreR);
                    var wristAngR = AngleDeg(foreDirR, handDirR);
                    if (prevWrR.HasValue) wristVelR.Add(Math.Abs(wristAngR - prevWrR.Value) / dtSeconds);
                    prevWrR = wristAngR;
                }
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

        private static double AngleDeg(in Vector3 a, in Vector3 b)
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

        // Percentile with linear interpolation between ranks (Excel PERCENTILE.INC style)
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
