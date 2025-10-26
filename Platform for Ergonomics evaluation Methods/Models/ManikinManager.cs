using Newtonsoft.Json;
using PEM.Utils;
using System.Diagnostics;

namespace PEM
{
    public static class ManikinManager
    {
        public static readonly Dictionary<string, ManikinBase> LoadedManikins = new();

        // The chosen/active manikin id (used by AFF/LAL/etc.)
        public static string? ActiveManikinId { get; private set; }

        // Convenience accessor
        public static ManikinBase? ActiveManikin =>
            ActiveManikinId != null && LoadedManikins.TryGetValue(ActiveManikinId, out var m) ? m : null;

        // Persist just the active id (and optionally known ids) to disk so you can restore across runs
        static string stateFile => Paths.Root + "manikin_state.json";

        class State
        {
            public string? ActiveId { get; set; }
            public List<string> KnownIds { get; set; } = new();
        }

        static void SaveState()
        {
            try
            {
                var s = new State
                {
                    ActiveId = ActiveManikinId,
                    KnownIds = LoadedManikins.Keys.ToList()
                };
                File.WriteAllText(stateFile, JsonConvert.SerializeObject(s, Formatting.Indented));
            }
            catch { /* ignore */ }
        }

        public static bool LoadLast()
        {
            try
            {
                if (File.Exists(stateFile))
                {
                    var s = JsonConvert.DeserializeObject<State>(File.ReadAllText(stateFile));
                    ActiveManikinId = s?.ActiveId;
                    return true;
                }
            }
            catch (Exception e) { Debug.WriteLine(e.Message); }
            return false;
        }

        public static string Add(string id, ManikinBase manikin, bool makeActive = true)
        {
            LoadedManikins[id] = manikin;
            if (makeActive) ActiveManikinId = id;
            Debug.WriteLine("Manikin loaded: " + manikin.GetDescriptiveName());
            SaveState();
            return id;
        }

        public static IEnumerable<(string id, string name)> List() =>
            LoadedManikins.Select(kv => (kv.Key, kv.Value.GetDescriptiveName()));

        public static bool Select(string id)
        {
            if (!LoadedManikins.ContainsKey(id)) return false;
            ActiveManikinId = id;
            SaveState();
            return true;
        }

        public static bool Remove(string id)
        {
            var removed = LoadedManikins.Remove(id);
            if (removed && ActiveManikinId == id)
            {
                ActiveManikinId = LoadedManikins.Keys.FirstOrDefault();
            }
            SaveState();
            return removed;
        }

        public static bool ParseMessage(string json)
        {
            try
            {
                dynamic message = JsonConvert.DeserializeObject(json);
                string parser = message?.parser;

                // Collect files from either 'manikinFilenames' (array) or legacy 'file' (string)
                var files = new List<string>();

                if (message?.manikinFilenames != null)
                {
                    foreach (var v in message.manikinfFilenames ?? message.manikinFilenames) // tolerate typo if any
                    {
                        var s = (v ?? "").ToString();
                        if (!string.IsNullOrWhiteSpace(s)) files.Add(s);
                    }
                }

                // Old shape: { parser: "...", file: "path" }
                if (message?.file != null)
                {
                    var s = (message.file ?? "").ToString();
                    if (!string.IsNullOrWhiteSpace(s)) files.Add(s);
                }

                if (files.Count == 0) return false;

                bool madeAny = false;

                if (string.Equals(parser, "XsensManikin", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var fn in files)
                    {
                        var manikin = new Xsens.XsensManikin(fn);
                        var id = Path.GetFileNameWithoutExtension(fn);

                        // Avoid id collisions if you upload multiple files with same basename
                        if (LoadedManikins.ContainsKey(id))
                            id = $"{id}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";

                        Add(id, manikin, makeActive: true);
                        madeAny = true;
                    }
                    return madeAny;
                }

                if (string.Equals(parser, "IMMAManikin", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var fn in files)
                    {
                        var manikin = new IMMA.IMMAManikin(fn);
                        var id = Path.GetFileNameWithoutExtension(fn);

                        if (LoadedManikins.ContainsKey(id))
                            id = $"{id}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";

                        Add(id, manikin, makeActive: true);
                        madeAny = true;
                    }
                    return madeAny;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return false;
        }

    }
}
