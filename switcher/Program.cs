using System;
using System.Net.NetworkInformation;

class Program
{
    static void Main()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Network Card Menu ===");
            Console.WriteLine("1. Show All Physical Network Cards");
            Console.WriteLine("2. Show Only Active (Connected) Cards");
            Console.WriteLine("3. Show Only WiFi Cards");
            Console.WriteLine("4. Show Only LAN (Ethernet) Cards");
            Console.WriteLine("0. Exit");
            Console.Write("Choose an option: ");

            string choice = Console.ReadLine();

            if (choice == "1")
            {
                ShowNetworkCards(FilterType.AllPhysical);
                ReturnToMenu();
            }
            else if (choice == "2")
            {
                ShowNetworkCards(FilterType.Active);
                ReturnToMenu();
            }
            else if (choice == "3")
            {
                ShowNetworkCards(FilterType.WiFi);
                ReturnToMenu();
            }
            else if (choice == "4")
            {
                ShowNetworkCards(FilterType.LAN);
                ReturnToMenu();
            }
            else if (choice == "0")
            {
                break;
            }
            else
            {
                Console.WriteLine("Invalid choice, press Enter to try again...");
                Console.ReadLine();
            }
        }
    }

    static void ReturnToMenu()
    {
        Console.WriteLine("\nPress Enter to return to menu...");
        Console.ReadLine();
    }

    enum FilterType
    {
        AllPhysical,
        Active,
        WiFi,
        LAN
    }

    static void ShowNetworkCards(FilterType filter)
    {
        Console.Clear();
        Console.WriteLine($"=== {filter} Network Cards ===");

        NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface adapter in adapters)
        {
            // فیلتر اولیه برای حذف کارت‌های مجازی
            if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                adapter.NetworkInterfaceType == NetworkInterfaceType.Tunnel ||
                adapter.Description.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0 ||
                adapter.Description.IndexOf("vpn", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            bool show = false;

            switch (filter)
            {
                case FilterType.AllPhysical:
                    show = true;
                    break;

                case FilterType.Active:
                    show = adapter.OperationalStatus == OperationalStatus.Up;
                    break;

                case FilterType.WiFi:
                    show = adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211;
                    break;

                case FilterType.LAN:
                    show = adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet;
                    break;
            }

            if (show)
            {
                Console.WriteLine($"Name: {adapter.Name}");
                Console.WriteLine($"Description: {adapter.Description}");
                Console.WriteLine($"Type: {adapter.NetworkInterfaceType}");
                Console.WriteLine($"Status: {adapter.OperationalStatus}");
                Console.WriteLine(new string('-', 40));
            }
        }
    }
}
