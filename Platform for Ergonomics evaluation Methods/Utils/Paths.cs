namespace PEM.Utils
{
    public class Paths
    {
        static string CreateIfMissing(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
        public static string Root
        {
            get
            {
                return CreateIfMissing(Environment.GetEnvironmentVariable("LOCALAPPDATA") + "/PEM/");
            }
        }
        public static string Uploads
        {
            get
            {
                return CreateIfMissing(Root + "Uploads/");
            }
        }
    }
}
