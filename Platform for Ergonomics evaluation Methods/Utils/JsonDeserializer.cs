using Newtonsoft.Json;
using Platform_for_Ergonomics_evaluation_Methods.Models;

namespace Platform_for_Ergonomics_evaluation_Methods.Utils
{
    public class JsonDeserializer
    {
        // Method to deserialize a JSON string into a Manikin object
        public Manikin DeserializeManikin(string jsonMessage)
        {
            try
            {
                // Deserialize JSON string into Manikin object
                Manikin manikin = JsonConvert.DeserializeObject<Manikin>(jsonMessage);
                return manikin;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to deserialize JSON: {ex.Message}");
                return null;
            }
        }

        // Method to process and log the Manikin data
        public void ProcessManikin(Manikin manikin)
        {
            if (manikin != null)
            {
                // Process the manikin object (e.g., log joint angles, forces, etc.)
                Console.WriteLine("Joint Angles:");
                foreach (var joint in manikin.JointAngles)
                {
                    Console.WriteLine($"{joint.Key}: {joint.Value}");
                }

                Console.WriteLine("Forces:");
                foreach (var force in manikin.Forces)
                {
                    Console.WriteLine($"{force.Key}: {force.Value}");
                }

                Console.WriteLine("Torques:");
                foreach (var torque in manikin.Torques)
                {
                    Console.WriteLine($"{torque.Key}: {torque.Value}");
                }
            }
            else
            {
                Console.WriteLine("Manikin object is null, cannot process.");
            }
        }
    }
}
