namespace PEM.Models
{
    public class Manikin
    {
        // Joint angles
        public Dictionary<string, float> JointAngles { get; set; }

        // Forces
        public Dictionary<string, float> Forces { get; set; }

        // Torques
        public Dictionary<string, float> Torques { get; set; }

        public Manikin()
        {
            JointAngles = new Dictionary<string, float>();
            Forces = new Dictionary<string, float>();
            Torques = new Dictionary<string, float>();
        }
    }
}
