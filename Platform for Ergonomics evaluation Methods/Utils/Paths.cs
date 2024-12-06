namespace Platform_for_Ergonomics_evaluation_Methods.Utils
{
    public class Paths
    {
        public static string Root
        {
            get {
                return Environment.GetEnvironmentVariable("LOCALAPPDATA")+"/PEM/";
            }
        }
    }
}
