using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml;

namespace Platform_for_Ergonomics_evaluation_Methods.Importers.Xsens
{
    public class XsensManikin
    {
        public XsensManikin()
        {
            Debug.WriteLine("Hej");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(File.ReadAllText("C:\\Downloads\\Emma-001.mvnx"));
            XmlNodeList joints = doc.GetElementsByTagName("joint");
            foreach (XmlElement joint in joints)
            {
                Debug.WriteLine(joint.GetAttribute("label"));

            }
            Debug.WriteLine("Joints.Count:" + joints.Count);
            XmlNodeList frames = doc.GetElementsByTagName("frame");
            XmlElement f = ((XmlElement)frames[0]);
            XmlNode pos = f.GetElementsByTagName("position")[0];
            string[] posStrings = pos.InnerText.Split(' ');
            Debug.WriteLine(posStrings.Length / 3);
            XmlNode orientations = f.GetElementsByTagName("orientation")[0];
            string[] oStrings = orientations.InnerText.Split(' ');
            Debug.WriteLine(oStrings.Length/4);
        }
    }
}
