﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;

namespace IntegrationDemo {


    class Program {
        public static string DEMO_DECK_ID = "7f18056c-7a2a-46ac-9956-662e7d0b78ec";
        //Deck pages
        static class DemoPages {
            public static string SEMAPHORE = "00d9c649-f2df-405f-abfd-06d38f8626be";
            public static string SHIELDS_AND_WEAPONS = "16856457-d2f8-405e-927e-749b1dc7d7ec";
        }
        // Button id's in our demo deck that we want to control
        static class DemoButtons {
            public static string SEMAPHORE_RED = "70158257-0124-4e3d-bc26-728a2d417109";
            public static string SEMAPHORE_GREEN = "cf3cd93c-e43c-4649-9f47-5441555ca7ce";
            public static string SEMAPHORE_YELLOW = "ba9a39d5-9cac-4d4e-94f0-b67fa0cbedc2";
            public static string SHIELDS_CRITICAL = "2aadc834-56f7-49e7-93d0-e047f772e7f9";
            public static string SHIELDS_PERCENT = "9ec833c2-dd31-40a8-b919-ca332fb97b51";
            public static string AMMO_COUNT = "ef59a421-e820-49e2-b223-b0efa9c95555";
        }


        public static string PIN = "";
        public static string CLIENT_ID;
        public static MatricIntegration mtrx;
        static void Main(string[] args)
        {
            Console.Title = "MATRIC Integration Demo";
            mtrx = new MatricIntegration();
            Console.WriteLine("Authorize connection in MATRIC, then enter PIN:");
            mtrx.Connect();
            PIN = Console.ReadLine();
            mtrx.GetConnectedClients();
            Console.ReadLine();
        }

        private static void StartDemo() {
            string GRAY = "#111111";
            string YELLOW = "yellow";
            string GREEN = "green";
            string RED = "red";
            string BLACK = "black";
            string WHITE = "white";

            mtrx.SetDeck(CLIENT_ID, DEMO_DECK_ID, DemoPages.SEMAPHORE);
            Thread.Sleep(800);
            mtrx.SetButtonProperties(CLIENT_ID, DemoButtons.SEMAPHORE_GREEN, backgroundcolorOff: GREEN);
            Thread.Sleep(800);
            mtrx.SetButtonProperties(CLIENT_ID, DemoButtons.SEMAPHORE_GREEN, backgroundcolorOff: GRAY);
            mtrx.SetButtonProperties(CLIENT_ID, DemoButtons.SEMAPHORE_YELLOW, backgroundcolorOff: YELLOW);
            Thread.Sleep(800);
            mtrx.SetButtonProperties(CLIENT_ID, DemoButtons.SEMAPHORE_GREEN, backgroundcolorOff: GRAY);
            mtrx.SetButtonProperties(CLIENT_ID, DemoButtons.SEMAPHORE_YELLOW, backgroundcolorOff: GRAY);
            mtrx.SetButtonProperties(CLIENT_ID, DemoButtons.SEMAPHORE_RED, backgroundcolorOff: RED);
            Thread.Sleep(1000);
            mtrx.SetActivePage(CLIENT_ID, DemoPages.SHIELDS_AND_WEAPONS);
            Thread.Sleep(1500);
            //simulate battle
            int ammo = 500;
            int shields = 100;
            for (int i = 0; i < 100; i++) {
                if (shields < 30)
                {
                    mtrx.SetButtonProperties(CLIENT_ID, DemoButtons.SHIELDS_CRITICAL, textcolorOff: BLACK, backgroundcolorOff: RED);
                    mtrx.SetButtonProperties(CLIENT_ID, DemoButtons.SHIELDS_PERCENT, backgroundcolorOff: GRAY, textcolorOff: RED, text: $@"{shields}%");
                }
                else if (shields < 50)
                {
                    mtrx.SetButtonProperties(CLIENT_ID, DemoButtons.SHIELDS_PERCENT, backgroundcolorOff: GRAY, textcolorOff: YELLOW, text: $@"{shields}%");
                }
                else {
                    mtrx.SetButtonProperties(CLIENT_ID, DemoButtons.SHIELDS_PERCENT, backgroundcolorOff: GRAY, textcolorOff: GREEN, text: $@"{shields}%");
                }
                mtrx.SetButtonProperties(CLIENT_ID, DemoButtons.AMMO_COUNT, backgroundcolorOff: GRAY, textcolorOff: WHITE, text: $@"{ammo}");
                shields--;
                ammo = ammo -5;
                Thread.Sleep(200);
            }

            //flash critical shields
            bool criticalOn = true;
            for (int i = 0; i < 20; i++) {
                if (criticalOn)
                {
                    mtrx.SetButtonProperties(CLIENT_ID, DemoButtons.SHIELDS_CRITICAL, textcolorOff: GRAY, backgroundcolorOff: BLACK);
                }
                else {
                    mtrx.SetButtonProperties(CLIENT_ID, DemoButtons.SHIELDS_CRITICAL, textcolorOff: GRAY, backgroundcolorOff: RED);
                }
                criticalOn = !criticalOn;
                Thread.Sleep(400);
            }
            Console.WriteLine("Demo complete");

        }


        public static void UpdateClientsList(string json) {
        JArray connectedClients = (JArray)JsonConvert.DeserializeObject(json);
        if (connectedClients.Count == 0)
        {
            Console.WriteLine("No connected devices found, make sure your smartphone/tablet is connected\nPress any key to exit");
            Console.ReadKey();
            Environment.Exit(0);
        }
        Console.WriteLine("Found devices:");
        foreach (JObject client in connectedClients)
        {
            Console.WriteLine($@"{client.GetValue("Hash")} {client.GetValue("Name")}");                
        }
            CLIENT_ID = ((JObject)connectedClients[0]).GetValue("Id").ToString();
            Console.WriteLine("Starting demo on first device");
            StartDemo();
        }
    }
}