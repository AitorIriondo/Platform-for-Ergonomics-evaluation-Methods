using System;
using System.Collections.Generic;
using System.Linq;

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

            // Build criterias once
            var crit = new ManikinCriterias(manikin);
            var dict = crit.ToSeriesDictionary();

            // --- Extract the angles we need ---
            var headFlex = dict.TryGetValue("neckAng", out var hf) ? hf : new List<double>();
            var uaLeft = dict.TryGetValue("leftUpperArmBackAng", out var ul) ? ul : new List<double>();
            var uaRight = dict.TryGetValue("rightUpperArmBackAng", out var ur) ? ur : new List<double>();
            var wrLeft = dict.TryGetValue("leftWristAng", out var wl) ? wl : new List<double>();
            var wrRight = dict.TryGetValue("rightWristAng", out var wr) ? wr : new List<double>();

            // --- Velocities (assume fixed Δt = 1/60) ---
            const double dt = 1.0 / 60.0;
            var uaVelL = ComputeVelocity(uaLeft, dt);
            var uaVelR = ComputeVelocity(uaRight, dt);
            var wrVelL = ComputeVelocity(wrLeft, dt);
            var wrVelR = ComputeVelocity(wrRight, dt);

            // --- Collect percentiles ---
            var outDict = new Dictionary<string, Dictionary<string, double>>();
            AddPercentiles(outDict, "HeadFlexion", headFlex);
            AddPercentiles(outDict, "UpperArmAngleLeft", uaLeft);
            AddPercentiles(outDict, "UpperArmAngleRight", uaRight);
            AddPercentiles(outDict, "UpperArmVelocityLeft", uaVelL);
            AddPercentiles(outDict, "UpperArmVelocityRight", uaVelR);
            AddPercentiles(outDict, "WristVelocityLeft", wrVelL);
            AddPercentiles(outDict, "WristVelocityRight", wrVelR);

            return outDict;
        }

        // --- helpers -----------------------------------------------------

        private static List<double> ComputeVelocity(IList<double> angles, double dt)
        {
            var vel = new List<double>(angles.Count);
            if (angles.Count == 0) return vel;

            vel.Add(0); // first frame = 0
            for (int i = 1; i < angles.Count; i++)
                vel.Add(Math.Abs(angles[i] - angles[i - 1]) / dt);

            return vel;
        }

        private static void AddPercentiles(Dictionary<string, Dictionary<string, double>> dst,
                                           string key, IList<double> series)
        {
            if (series == null || series.Count == 0)
            {
                dst[key] = new Dictionary<string, double> { { "p10", 0 }, { "p50", 0 }, { "p90", 0 } };
                return;
            }
            var sorted = series.OrderBy(x => x).ToList();
            dst[key] = new Dictionary<string, double>
            {
                { "p10", GetPercentile(sorted, 10) },
                { "p50", GetPercentile(sorted, 50) },
                { "p90", GetPercentile(sorted, 90) },
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
