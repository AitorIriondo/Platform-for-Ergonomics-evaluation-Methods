using IMMA;
using System.Diagnostics;

namespace Platform_for_Ergonomics_evaluation_Methods
{
    public class ManikinManager
    {
        public static ManikinBase? loadedManikin = null;
        public static bool ParseMessage(string json)
        {
            if (json != null)
            {
                json = json.Trim();
                if (json.StartsWith("{") || json.StartsWith("["))
                {
                    ManikinBase m = IMMAManikin.TryParse(json);
                    if (m != null)
                    {
                        loadedManikin = m;
                        Debug.WriteLine("Manikin loaded: " + loadedManikin.ToString());
                    }
                }
            }

            return true;
        }
    }
}
