using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using System.Text.RegularExpressions;

class Program
{
    class Adapter
    {
        public string Name;
        public string Status;
        public string MAC;
        public bool IsPhysical;
        public List<string> IPv4 = new List<string>();
        public List<string> IPv6 = new List<string>();
        public List<string> DNS = new List<string>();
    }

    static void Main()
    {
        while (true)
        {
            Console.Clear();
            List<Adapter> adapters = GetAllAdapters();

            if (adapters.Count == 0)
            {
                Console.WriteLine("No network adapters found.");
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return;
            }

            PrintTable(adapters);

            Console.WriteLine("0. Exit");
            Console.Write("Select a card: ");
            string input = Console.ReadLine();
            if (input == "0") break;

            int selectedIndex;
            if (!int.TryParse(input, out selectedIndex) || selectedIndex < 1 || selectedIndex > adapters.Count)
            {
                Console.WriteLine("Invalid choice. Press Enter...");
                Console.ReadLine();
                continue;
            }

            AdapterMenu(adapters[selectedIndex - 1]);
        }
    }

    static List<Adapter> GetAllAdapters()
    {
        List<Adapter> list = new List<Adapter>();
        NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

        foreach (NetworkInterface nic in nics)
        {
            Adapter a = new Adapter();
            a.Name = nic.Name;
            a.Status = nic.OperationalStatus == OperationalStatus.Up ? "Enabled" : "Disabled";
            a.MAC = nic.GetPhysicalAddress().ToString();
            a.IsPhysical = nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                           nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                           !nic.Description.ToLower().Contains("virtual") &&
                           !nic.Description.ToLower().Contains("vpn");

            IPInterfaceProperties props = nic.GetIPProperties();

            foreach (UnicastIPAddressInformation ip in props.UnicastAddresses)
            {
                if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    a.IPv4.Add(ip.Address.ToString());
                else if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    a.IPv6.Add(ip.Address.ToString());
            }

            foreach (IPAddress dns in props.DnsAddresses)
            {
                a.DNS.Add(dns.ToString());
            }

            list.Add(a);
        }

        return list;
    }

    static void PrintTable(List<Adapter> adapters)
    {
        Console.WriteLine("=== Network Adapters ===");
        string line = "+----+-------------------------+----------+-------------------+-------------------+-------------------+";
        Console.WriteLine(line);
        Console.WriteLine("| {0,-2} | {1,-23} | {2,-8} | {3,-17} | {4,-17} | {5,-17} |", "No", "Name", "Status", "IPv4", "IPv6", "DNS");
        Console.WriteLine(line);

        for (int i = 0; i < adapters.Count; i++)
        {
            Adapter a = adapters[i];

            // رنگ کارت
            ConsoleColor color = a.IsPhysical ? ConsoleColor.Cyan : ConsoleColor.Yellow;
            Console.ForegroundColor = color;

            // وضعیت کارت
            string status = a.Status;
            ConsoleColor statusColor = status.Equals("Enabled", StringComparison.OrdinalIgnoreCase) ? ConsoleColor.Green : ConsoleColor.Red;

            // چاپ ردیف با رنگ وضعیت
            Console.Write("| {0,-2} | {1,-23} | ", i + 1, a.Name);
            Console.ForegroundColor = statusColor;
            Console.Write("{0,-8}", status);
            Console.ForegroundColor = color;
            Console.Write(" | {0,-17} | {1,-17} | {2,-17} |",
                a.IPv4.Count > 0 ? a.IPv4[0] : "-",
                a.IPv6.Count > 0 ? a.IPv6[0] : "-",
                a.DNS.Count > 0 ? a.DNS[0] : "-");
            Console.WriteLine();
            Console.WriteLine(line);
            Console.ResetColor();
        }
    }

    static void AdapterMenu(Adapter adapter)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Adapter: {0} ===", adapter.Name);
            Console.WriteLine("Status: {0}", adapter.Status);
            Console.WriteLine("MAC: {0}", adapter.MAC);
            Console.WriteLine("IPv4: {0}", adapter.IPv4.Count > 0 ? string.Join(",", adapter.IPv4) : "-");
            Console.WriteLine("IPv6: {0}", adapter.IPv6.Count > 0 ? string.Join(",", adapter.IPv6) : "-");
            Console.WriteLine("DNS: {0}", adapter.DNS.Count > 0 ? string.Join(",", adapter.DNS) : "-");
            Console.WriteLine();
            Console.WriteLine("1. Enable / Disable");
            Console.WriteLine("2. Set IP");
            Console.WriteLine("3. Set DNS");
            Console.WriteLine("0. Back");
            Console.Write("Choose: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1": ToggleAdapter(adapter); break;
                case "2": SetIP(adapter); break;
                case "3": SetDNS(adapter); break;
                case "0": return;
                default:
                    Console.WriteLine("Invalid choice. Press Enter...");
                    Console.ReadLine();
                    break;
            }
        }
    }

    static void ToggleAdapter(Adapter adapter)
    {
        string command = "interface set interface \"" + adapter.Name + "\" ";
        if (adapter.Status.Equals("Enabled", StringComparison.OrdinalIgnoreCase))
        {
            command += "disable";
            adapter.Status = "Disabled";
        }
        else
        {
            command += "enable";
            adapter.Status = "Enabled";
        }

        RunNetsh(command);
        Console.WriteLine("Done. Press Enter...");
        Console.ReadLine();
    }

    static void SetIP(Adapter adapter)
    {
        Console.Clear();
        Console.WriteLine("1. Automatic (DHCP)");
        Console.WriteLine("2. Manual");
        Console.Write("Choose: ");
        string choice = Console.ReadLine();

        if (choice == "1")
        {
            RunNetsh("interface ip set address name=\"" + adapter.Name + "\" dhcp");
            adapter.IPv4.Clear();
            Console.WriteLine("IP set to automatic. Press Enter...");
            Console.ReadLine();
        }
        else if (choice == "2")
        {
            string ip = "", mask = "", gateway = "";
            while (true)
            {
                Console.Write("Enter IP: ");
                ip = Console.ReadLine();
                if (IsValidIP(ip)) break;
                Console.WriteLine("Invalid IP format.");
            }
            while (true)
            {
                Console.Write("Enter Subnet Mask: ");
                mask = Console.ReadLine();
                if (IsValidIP(mask)) break;
                Console.WriteLine("Invalid mask.");
            }
            while (true)
            {
                Console.Write("Enter Gateway: ");
                gateway = Console.ReadLine();
                if (IsValidIP(gateway)) break;
                Console.WriteLine("Invalid gateway.");
            }

            RunNetsh(string.Format("interface ip set address name=\"{0}\" static {1} {2} {3}", adapter.Name, ip, mask, gateway));
            adapter.IPv4.Clear();
            adapter.IPv4.Add(ip);
            Console.WriteLine("Manual IP set. Press Enter...");
            Console.ReadLine();
        }
    }

    static void SetDNS(Adapter adapter)
    {
        Console.Clear();
        Console.WriteLine("1. 8.8.8.8 , 1.1.1.1");
        Console.WriteLine("2. 4.2.2.4 , 8.8.8.8");
        Console.WriteLine("3. 178.22.122.100 , 185.51.200.2");
        Console.WriteLine("4. Automatic (DHCP)");
        Console.Write("Choose: ");
        string choice = Console.ReadLine();

        adapter.DNS.Clear();
        switch (choice)
        {
            case "1":
                RunNetsh("interface ip set dns name=\"" + adapter.Name + "\" static 8.8.8.8 primary");
                RunNetsh("interface ip add dns name=\"" + adapter.Name + "\" 1.1.1.1 index=2");
                adapter.DNS.Add("8.8.8.8");
                adapter.DNS.Add("1.1.1.1");
                break;
            case "2":
                RunNetsh("interface ip set dns name=\"" + adapter.Name + "\" static 4.2.2.4 primary");
                RunNetsh("interface ip add dns name=\"" + adapter.Name + "\" 8.8.8.8 index=2");
                adapter.DNS.Add("4.2.2.4");
                adapter.DNS.Add("8.8.8.8");
                break;
            case "3":
                RunNetsh("interface ip set dns name=\"" + adapter.Name + "\" static 178.22.122.100 primary");
                RunNetsh("interface ip add dns name=\"" + adapter.Name + "\" 185.51.200.2 index=2");
                adapter.DNS.Add("178.22.122.100");
                adapter.DNS.Add("185.51.200.2");
                break;
            case "4":
                RunNetsh("interface ip set dns name=\"" + adapter.Name + "\" dhcp");
                break;
            default:
                Console.WriteLine("Invalid choice. Press Enter...");
                Console.ReadLine();
                return;
        }

        Console.WriteLine("DNS applied. Press Enter...");
        Console.ReadLine();
    }

    static void RunNetsh(string command)
    {
        ProcessStartInfo psi = new ProcessStartInfo("netsh", command);
        psi.UseShellExecute = true;
        psi.Verb = "runas";
        psi.CreateNoWindow = true;

        Process proc = Process.Start(psi);
        proc.WaitForExit();
    }

    static bool IsValidIP(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return false;
        string pattern = @"^(\d{1,3}\.){3}\d{1,3}$";
        if (!Regex.IsMatch(ip, pattern)) return false;

        string[] parts = ip.Split('.');
        for (int i = 0; i < 4; i++)
        {
            int val;
            if (!int.TryParse(parts[i], out val)) return false;
            if (val < 0 || val > 255) return false;
        }

        return true;
    }
}
