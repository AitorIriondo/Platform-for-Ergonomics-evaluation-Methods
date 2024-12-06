using Platform_for_Ergonomics_evaluation_Methods.Utils;
using System.Diagnostics;

namespace Platform_for_Ergonomics_evaluation_Methods
{
    public class ManikinManager
    {
        public static ManikinBase? loadedManikin = null;
        static string lastParsedJsonFilename { get { return Paths.Root + "lastParsed.json"; } }
        public static bool LoadLast()
        {
            if (File.Exists(lastParsedJsonFilename))
            {
                return ParseMessage(File.ReadAllText(lastParsedJsonFilename));
            }
            return false;
        }
        static ManikinBase? TryParse(string json)
        {
            ManikinBase m = IMMA.IMMAManikin.TryParse(json);
            if (m != null)
            {
                return m;
            }
            return null;
        }
        public static bool ParseMessage(string json)
        {
            if (json != null)
            {
                json = json.Trim();
                if (json.StartsWith("{") || json.StartsWith("["))
                {
                    ManikinBase m = TryParse(json);
                    if (m != null)
                    {
                        loadedManikin = m;
                        Debug.WriteLine("Manikin loaded: " + loadedManikin.ToString());
                        File.WriteAllText(lastParsedJsonFilename, json);
                    }
                }
            }

            return true;
        }
    }
}
