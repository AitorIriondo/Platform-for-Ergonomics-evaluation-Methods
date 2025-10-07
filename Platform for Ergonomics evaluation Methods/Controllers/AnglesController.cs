using Microsoft.AspNetCore.Mvc;
using PEM.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json.Linq;

namespace PEM.Controllers
{
    public partial class AnglesController : Controller
    {
        // Serve the UI page
        [HttpGet]
        public IActionResult Index() => View();

        // Returns all available angles (computed + imported components)
        [HttpGet("/api/angles/available")]
        public IActionResult GetAvailableAngles([FromQuery] string manikinId = null)
        {
            var manikin = ResolveManikin(manikinId);
            if (manikin == null)
                return BadRequest("No manikin loaded.");

            var mName = manikin.GetDescriptiveName() ?? manikinId ?? "Manikin";

            var list = new List<object>();

            // --- 1. Computed criterias ---
            var crit = new ManikinCriterias(manikin);
            foreach (var key in crit.ToSeriesDictionary().Keys)
            {
                var raw = ManikinCriterias.HumanLabels.TryGetValue(key, out var lbl) ? lbl : key;
                list.Add(new { key, label = $"{mName} • {raw}" });
            }

            // --- 2. Imported MVNX joint angles ---
            if (manikin is Xsens.XsensManikin xs && xs.jointAnglesByName.Count > 0)
            {
                foreach (var jointName in xs.jointAnglesByName.Keys.OrderBy(k => k))
                {
                    foreach (var comp in new[] { "X", "Y", "Z" })
                    {
                        list.Add(new
                        {
                            key = $"IMP_{jointName}_{comp}",
                            label = $"{mName} • (IMP) {jointName} [{comp}]"
                        });
                    }
                }
            }

            return Ok(list);
        }

        // Returns one selected angle time series
        [HttpGet("/api/angles/series")]
        public IActionResult GetSeries([FromQuery] string manikinId, [FromQuery] string angle)
        {
            var manikin = ResolveManikin(manikinId);
            if (manikin == null)
                return BadRequest("No manikin loaded.");

            if (string.IsNullOrWhiteSpace(angle))
                return BadRequest("Missing 'angle' parameter.");

            try
            {
                // --- Imported component path ---
                if (angle.StartsWith("IMP_", StringComparison.OrdinalIgnoreCase))
                {
                    var mName = manikin.GetDescriptiveName() ?? manikinId ?? "Manikin";
                    // Example key: IMP_jRightShoulder_X
                    var parts = angle.Split('_', 3);
                    if (parts.Length < 3)
                        return BadRequest("Invalid imported angle key format.");

                    var jointName = parts[1];
                    var comp = parts[2].ToUpperInvariant();

                    if (manikin is Xsens.XsensManikin xs &&
                        xs.jointAnglesByName.TryGetValue(jointName, out var vectors))
                    {
                        Func<Vector3, double> selector = comp switch
                        {
                            "X" => v => v.X,
                            "Y" => v => v.Y,
                            "Z" => v => v.Z,
                            _ => v => 0
                        };

                        var n = Math.Min(vectors.Count, manikin.postureTimeSteps.Count);
                        var time = manikin.postureTimeSteps.Take(n).ToList();
                        var values = new List<double>(n);
                        for (int i = 0; i < n; i++)
                        {
                            var val = selector(vectors[i]);
                            if (double.IsNaN(val) || double.IsInfinity(val))
                                val = 0.0;
                            values.Add(val);
                        }

                        return Ok(new
                        {
                            manikinId,
                            angle,
                            label = $"{mName} • (IMP) {jointName} [{comp}]",
                            time,
                            values
                        });
                    }

                    return NotFound($"Imported joint '{jointName}' not found.");
                }

                // --- Computed criterias path ---
                var crit = new ManikinCriterias(manikin);
                var dict = crit.ToSeriesDictionary();
                if (!dict.TryGetValue(angle, out var valuesComputed))
                    return NotFound($"Angle '{angle}' not found.");

                var human = ManikinCriterias.HumanLabels.TryGetValue(angle, out var lbl) ? lbl : angle;
                var labelComputed = $"{(manikin.GetDescriptiveName() ?? manikinId)} • {human}";

                return Ok(new
                {
                    manikinId,
                    angle,
                    label = labelComputed,
                    time = crit.Time,
                    values = valuesComputed
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Helper
        private static ManikinBase ResolveManikin(string manikinId)
        {
            if (!string.IsNullOrWhiteSpace(manikinId))
            {
                if (ManikinManager.LoadedManikins.TryGetValue(manikinId, out var m))
                    return m;
                return null;
            }
            return ManikinManager.ActiveManikin;
        }
    }
}
