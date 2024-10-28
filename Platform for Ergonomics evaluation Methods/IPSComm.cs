using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace Platform_for_Ergonomics_evaluation_Methods
{
    public class IPSComm
    {
        protected bool listening;
        protected List<string> filesDone = new List<string>();
        protected Dictionary<string, int> fileErrorCounts = new Dictionary<string, int>();
        const string ERROR_HEADER_TOO_SMALL = "HEADER_TOO_SMALL";
        const string ERROR_HEADER_UNPARSEABLE = "HEADER_UNPARSEABLE";
        const string ERROR_PAYLOAD_SIZE_MISMATCH = "PAYLOAD_SIZE_MISMATCH";
        protected static IPSComm instance = new IPSComm();
        protected string ReadFile(string filename, out string error)
        {
            FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            int headerLen = 10;
            int len = (int)fs.Length;
            error = null;
            if (len < headerLen)
            {
                error = ERROR_HEADER_TOO_SMALL;
                return "";
            }
            byte[] headerBuf = new byte[headerLen];
            fs.Read(headerBuf, 0, headerLen);
            int payloadLen;
            if (!int.TryParse(Encoding.UTF8.GetString(headerBuf), out payloadLen))
            {
                error = ERROR_HEADER_UNPARSEABLE;
                return "";
            }
            if (len != headerLen + payloadLen)
            {
                error = ERROR_PAYLOAD_SIZE_MISMATCH;
                return "";
            }
            byte[] payload = new byte[payloadLen];
            fs.Read(payload, 0, payloadLen);
            fs.Close();
            return Encoding.UTF8.GetString(payload);
        }
        protected void TruncateFile(string filename)
        {
            FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            fs.SetLength(0);
            fs.Close();
        }
        protected void Send(string str)
        {
            try
            {
                //"localhost" takes about 2 seconds to initialize. "127.0.0.1" takes milliseconds.
                TcpClient client = new TcpClient("127.0.0.1", 5000);
                byte[] buf = Encoding.UTF8.GetBytes(str);
                NetworkStream stream = client.GetStream();
                stream.Write(buf, 0, buf.Length);
                stream.Flush();
                Debug.WriteLine($"Sent: {str}");
                
                buf = new byte[4096];
                //stream.Read(buf, 0, buf.Length);
                string response = Encoding.UTF8.GetString(buf);
                Debug.WriteLine($"Received: {response}");
                
                stream.Close();
                client.Close();
            }
            catch (ArgumentNullException e)
            {
                Debug.WriteLine($"ArgumentNullException: {e}");
            }
            catch (SocketException e)
            {
                Debug.WriteLine($"SocketException: {e}");
            }
        }

        public static void Start()
        {
            Thread thread = new Thread(() =>
            {
                instance.Listen();
            });
            thread.Start();
        }
        public static void Stop()
        {
            instance.listening = false;
        }
        protected void Listen()
        {
            string peemDir = Environment.GetEnvironmentVariable("USERPROFILE") + "\\AppData\\Local\\Peem\\";
            string comDir = peemDir + "ipscomm_tmp\\";

            //Handle old files
            foreach (string filename in Directory.GetFiles(comDir, "*.txt"))
            {
                FileInfo fi = new FileInfo(filename);
                Debug.WriteLine(fi.Length);
                if (fi.Length == 0)
                {
                    try
                    {
                        File.Delete(filename);
                    }
                    catch (Exception ex)
                    {
                        filesDone.Add(filename);
                    }
                }
            }

            listening = true;
            while (listening)
            {
                try
                {
                    List<string> filenames = new List<string>(Directory.GetFiles(comDir, "*.txt"));
                    filenames.Sort();
                    //We need a delay in the loop. By putting it here, new files can be populated while we wait.
                    System.Threading.Thread.Sleep(10);
                    foreach (string filename in filenames)
                    {
                        if (!filesDone.Contains(filename))
                        {
                            string error;
                            string txt = ReadFile(filename, out error);
                            Debug.WriteLine(filename);
                            Debug.WriteLine("txt:" + txt);
                            if (error == null)
                            {
                                Send(txt);
                                try
                                {
                                    File.Delete(filename);
                                }
                                catch (Exception)
                                {
                                    filesDone.Add(filename);
                                    TruncateFile(filename);
                                }
                            }
                            else
                            {
                                Debug.WriteLine("error:" + error);
                                if (!fileErrorCounts.ContainsKey(filename))
                                {
                                    fileErrorCounts.Add(filename, 1);
                                }
                                if (fileErrorCounts[filename] >= 2)
                                {
                                    Debug.WriteLine("Too many errors, skipping file");
                                    filesDone.Add(filename);
                                    fileErrorCounts.Remove(filename);
                                }
                                else
                                {
                                    fileErrorCounts[filename]++;
                                }
                            }
                            Debug.WriteLine("-----");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    Thread.Sleep(100);
                }
            }
        }
    }
}
