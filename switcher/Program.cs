using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

class Program
{
    class Adapter
    {
        public string Name;
        public string Status;
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

            Console.WriteLine("=== Network Adapters ===");
            for (int i = 0; i < adapters.Count; i++)
            {
                Console.WriteLine("{0}. {1} ({2})", i + 1, adapters[i].Name, adapters[i].Status);
            }
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

            Adapter selectedAdapter = adapters[selectedIndex - 1];
            AdapterMenu(selectedAdapter);
        }
    }

    static List<Adapter> GetAllAdapters()
    {
        List<Adapter> list = new List<Adapter>();

        ProcessStartInfo psi = new ProcessStartInfo("netsh", "interface show interface");
        psi.RedirectStandardOutput = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        Process proc = Process.Start(psi);
        string output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();

        string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 3; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] parts = Regex.Split(line, @"\s{2,}");
            if (parts.Length < 4) continue;

            Adapter a = new Adapter();
            a.Status = parts[0]; // Enabled / Disabled
            a.Name = parts[3];
            list.Add(a);
        }

        return list;
    }

    static void AdapterMenu(Adapter adapter)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Adapter: {0} ===", adapter.Name);
            Console.WriteLine("1. Enable / Disable");
            Console.WriteLine("2. Set IP");
            Console.WriteLine("3. Set DNS");
            Console.WriteLine("0. Back");
            Console.Write("Choose: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ToggleAdapter(adapter);
                    break;
                case "2":
                    SetIP(adapter);
                    break;
                case "3":
                    SetDNS(adapter);
                    break;
                case "0":
                    return;
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

        switch (choice)
        {
            case "1":
                RunNetsh("interface ip set dns name=\"" + adapter.Name + "\" static 8.8.8.8 primary");
                RunNetsh("interface ip add dns name=\"" + adapter.Name + "\" 1.1.1.1 index=2");
                break;
            case "2":
                RunNetsh("interface ip set dns name=\"" + adapter.Name + "\" static 4.2.2.4 primary");
                RunNetsh("interface ip add dns name=\"" + adapter.Name + "\" 8.8.8.8 index=2");
                break;
            case "3":
                RunNetsh("interface ip set dns name=\"" + adapter.Name + "\" static 178.22.122.100 primary");
                RunNetsh("interface ip add dns name=\"" + adapter.Name + "\" 185.51.200.2 index=2");
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
