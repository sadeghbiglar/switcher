// Program.cs
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

class Program
{
    static void Main()
    {
        try
        {
            var adapters = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(ni =>
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                     ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    &&
                    ni.OperationalStatus != OperationalStatus.Unknown
                    &&
                    !IsVirtual(ni)
                )
                .ToArray();

            if (adapters.Length == 0)
            {
                Console.WriteLine("No physical network adapters found.");
            }
            else
            {
                int idx = 1;
                foreach (var ni in adapters)
                {
                    Console.WriteLine(new string('=', 60));
                    Console.WriteLine($"Physical Adapter #{idx++}");
                    Console.WriteLine($"Name:        {ni.Name}");
                    Console.WriteLine($"Description: {ni.Description}");
                    Console.WriteLine($"Type:        {ni.NetworkInterfaceType}");
                    Console.WriteLine($"Status:      {ni.OperationalStatus}");
                    Console.WriteLine($"Speed:       {FormatSpeed(ni.Speed)}");
                    Console.WriteLine($"MAC:         {FormatMac(ni.GetPhysicalAddress())}");

                    var ipProps = ni.GetIPProperties();

                    var unicast = ipProps.UnicastAddresses;
                    if (unicast.Count > 0)
                    {
                        Console.WriteLine("IP Addresses:");
                        foreach (var ua in unicast)
                        {
                            string family = ua.Address.AddressFamily == AddressFamily.InterNetwork ? "IPv4" :
                                            ua.Address.AddressFamily == AddressFamily.InterNetworkV6 ? "IPv6" :
                                            ua.Address.AddressFamily.ToString();
                            Console.WriteLine($"  - {family}: {ua.Address}");
                        }
                    }

                    var gateways = ipProps.GatewayAddresses;
                    if (gateways.Count > 0)
                    {
                        Console.WriteLine("Gateways:");
                        foreach (var g in gateways)
                            Console.WriteLine($"  - {g.Address}");
                    }

                    Console.WriteLine(new string('=', 60));
                    Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error while reading physical network adapters: " + ex.Message);
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static bool IsVirtual(NetworkInterface ni)
    {
        string desc = ni.Description.ToLower();
        string name = ni.Name.ToLower();

        // لیست کلمات کلیدی کارت‌های مجازی
        string[] virtualKeywords = {
            "virtual", "vpn", "loopback", "tunneling",
            "pseudo", "vmware", "hyper-v", "bluetooth"
        };

        return virtualKeywords.Any(k => desc.Contains(k) || name.Contains(k));
    }

    static string FormatMac(PhysicalAddress pa)
    {
        if (pa == null) return "N/A";
        var bytes = pa.GetAddressBytes();
        if (bytes.Length == 0) return "N/A";
        return string.Join(":", bytes.Select(b => b.ToString("X2")));
    }

    static string FormatSpeed(long speedBitsPerSecond)
    {
        if (speedBitsPerSecond <= 0) return "Unknown";
        double bits = speedBitsPerSecond;
        string[] units = { "bps", "Kbps", "Mbps", "Gbps", "Tbps" };
        int u = 0;
        while (bits >= 1000 && u < units.Length - 1)
        {
            bits /= 1000;
            u++;
        }
        return $"{bits:N2} {units[u]}";
    }
}
