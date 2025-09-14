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
            List<Adapter> adapters = GetActiveAdapters();

            if (adapters.Count == 0)
            {
                Console.WriteLine("No active network adapters found.");
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("=== Active Network Adapters ===");
            for (int i = 0; i < adapters.Count; i++)
            {
                Console.WriteLine("{0}. {1}", i + 1, adapters[i].Name);
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
            SetDNS(selectedAdapter);
        }
    }

    static List<Adapter> GetActiveAdapters()
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

            string status = parts[0];
            string name = parts[3];

            if (status.Equals("Enabled", StringComparison.OrdinalIgnoreCase))
            {
                Adapter a = new Adapter();
                a.Name = name;
                a.Status = status;
                list.Add(a);
            }
        }

        return list;
    }

    static void SetDNS(Adapter adapter)
    {
        Console.Clear();
        Console.WriteLine("=== Set DNS for {0} ===", adapter.Name);
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

        Console.WriteLine("DNS applied. Press Enter to continue...");
        Console.ReadLine();
    }

    static void RunNetsh(string command)
    {
        ProcessStartInfo psi = new ProcessStartInfo("netsh", command);
        psi.UseShellExecute = true; // لازم است برای دسترسی admin
        psi.Verb = "runas";
        psi.CreateNoWindow = true;

        Process proc = Process.Start(psi);
        proc.WaitForExit();
    }
}
