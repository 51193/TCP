namespace TCP
{
    internal class Program
    {
        static void Main(string[] args)
        {
            WebDevice webDevice = WebDevice.Instance;
            webDevice.DisplayAvailableDevices();
            int index = int.Parse(Console.ReadLine());
            webDevice.SetDeviceInUse(index);
            webDevice.SetRecordFile("capture.pcap");
            webDevice.StartCapture();

            TCPController controller = TCPController.Instance;
            controller.StartNewConnection("config.txt");

            Console.ReadKey(true);
            controller.SendDataPacket("config.txt", "payload.txt");

            Console.ReadKey(true);
            webDevice.EndCapture();
        }
    }
}
