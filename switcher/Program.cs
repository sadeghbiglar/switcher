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
            Console.WriteLine("1. Show Physical Network Cards");
            Console.WriteLine("0. Exit");
            Console.Write("Choose an option: ");

            string choice = Console.ReadLine();

            if (choice == "1")
            {
                ShowNetworkCards();
                Console.WriteLine("\nPress Enter to return to menu...");
                Console.ReadLine();
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

    static void ShowNetworkCards()
    {
        Console.Clear();
        Console.WriteLine("=== Physical Network Cards ===");

        NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface adapter in adapters)
        {
            // فقط کارت‌های فیزیکی (نه لوکال و نه مجازی)
            if (adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                adapter.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                adapter.Description.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) < 0 &&
                adapter.Description.IndexOf("vpn", StringComparison.OrdinalIgnoreCase) < 0)
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
