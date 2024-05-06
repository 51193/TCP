using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace TCP
{
    public class WebDevice
    {
        private static WebDevice? instance;
        private CaptureDeviceList? devices;
        private ILiveDevice? device;
        private CaptureFileWriterDevice? recorder;

        private int deviceUsage = 0;

        public static WebDevice Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new WebDevice();
                }
                return instance;
            }
        }

        private WebDevice()
        {
            GetDevices();
        }

        private void IncrementDeviceUsage()
        {
            if (device != null)
            {
                int original = Interlocked.Increment(ref deviceUsage);
                if (original == 1 && device != null)
                {
                    device.Open(DeviceModes.Promiscuous);  // 确保设备被打开
                }
            }
            else
            {
                Console.WriteLine("Have not set device yet.");
            }
        }

        private void DecrementDeviceUsage()
        {
            if (device != null)
            {
                int remaining = Interlocked.Decrement(ref deviceUsage);
                if (remaining == 0)
                {
                    device.Close();  // 只有当没有更多操作时才关闭设备
                }
            }
            else
            {
                Console.WriteLine("Have not set device yet.");
            }
        }

        public void GetDevices()
        {
            devices = CaptureDeviceList.Instance;
            if (devices.Count < 1)
            {
                Console.WriteLine("No devices were found.");
            }
        }

        public void SetDeviceInUse(int index)
        {
            if (devices != null && devices.Count > index && Interlocked.CompareExchange(ref deviceUsage, 0, 0) == 0)
            {
                device = devices[index];
                Console.WriteLine($"Device {index} is set for use.");
            }
            else
            {
                Console.WriteLine("Invalid device index or device is in use.");
            }
        }

        public void SendPacket(byte[] pack)
        {
            if (device != null)
            {
                try
                {
                    IncrementDeviceUsage();
                    device.SendPacket(pack);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    DecrementDeviceUsage();
                }
            }
            else
            {
                Console.WriteLine("No available device to send the packet.");
            }
        }

        public void DisplayAvailableDevices()
        {
            if (devices == null || devices.Count == 0)
            {
                Console.WriteLine("No devices available.");
                return;
            }

            Console.WriteLine("Available Devices:");
            int index = 0;
            foreach (var dev in devices)
            {
                try
                {
                    //dev.Open();
                    Console.WriteLine($"{index++}: {dev.Name} - {dev.Description}");
                    if (dev.MacAddress != null)
                    {
                        Console.WriteLine($"MAC Address: {dev.MacAddress}");
                    }

                    var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                        .FirstOrDefault(ni => ni.GetPhysicalAddress().ToString() == dev.MacAddress?.ToString());
                    if (networkInterface != null)
                    {
                        var ipProps = networkInterface.GetIPProperties();
                        var ipAddress = ipProps.UnicastAddresses
                            .Where(ua => ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            .FirstOrDefault()?.Address.ToString();
                        var gatewayIPAddress = ipProps.GatewayAddresses
                            .FirstOrDefault()?.Address.ToString();

                        Console.WriteLine($"IP Address: {ipAddress ?? "N/A"}");
                        Console.WriteLine($"Gateway MAC Address: {GetGatewayMacAddress(gatewayIPAddress)}");
                        Console.WriteLine($"Gateway IP Address: {gatewayIPAddress ?? "N/A"}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    //dev.Close();
                    Console.WriteLine();
                }
            }
        }

        public string GetGatewayMacAddress(string? IPAddress)
        {
            if (string.IsNullOrEmpty(IPAddress))
            {
                return "N/A";
            }
            ProcessStartInfo psi = new ProcessStartInfo("arp", "-a")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };


            var process = Process.Start(psi);
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            string[] lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains($" {IPAddress} "))
                {
                    var parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                        return parts[1];
                }
            }

            return "N/A";
        }

        public void SetRecordFile(string filename)
        {
            recorder = new CaptureFileWriterDevice(filename);
        }

        public void RecordPacket(RawCapture packet)
        {
            if (recorder != null)
            {
                recorder.Write(packet);
            }
            else
            {
                Console.WriteLine("Recorder not set.");
            }
        }

        public void StartCapture()
        {
            if (recorder != null && device != null)
            {
                Console.WriteLine("Attempting to start capture on " + device.Description);
                try
                {
                    IncrementDeviceUsage();
                    recorder.Open();
                    device.OnPacketArrival += (sender, packet) =>
                    {
                        PacketParser.AnalyzePacket(packet);
                    };
                    device.StartCapture();
                }
                catch(Exception e)
                {
                    Console.WriteLine("Failed to start capture: " + e.Message);
                }
            }
            else
            {
                Console.WriteLine("Recorder not set or Invalid device.");
            }
        }

        public void EndCapture()
        {
            if (device != null && recorder != null)
            {
                recorder.Close();
                device.OnPacketArrival -= (sender, packet) =>
                {
                    PacketParser.AnalyzePacket(packet);
                };
                device.StopCapture();
                DecrementDeviceUsage();
                Console.WriteLine("Stop listen.");
            }
            else
            {
                Console.WriteLine("Have not set record file or Invalid device.");
            }
        }
    }
}
