﻿using System;
using System.Collections.Generic;
using System.Threading;
using Matric.Integration;

namespace IntegrationDemo {


    class Program {
        public static string DEMO_DECK_ID = "7f18056c-7a2a-46ac-9956-662e7d0b78ec";
        public static string SMILE_WINK = "😉";
        static string AppName = "MATRIC Integration Demo";
        public static string PIN = "";
        public static string CLIENT_ID;
        static Matric.Integration.Matric matric;

        //Deck pages
        static class DemoPages {
            public static string SEMAPHORE = "00d9c649-f2df-405f-abfd-06d38f8626be";
            public static string SHIELDS_AND_WEAPONS = "16856457-d2f8-405e-927e-749b1dc7d7ec";
            public static string DEMO_COMPLETE = "b0a0b559-9e2c-42af-9c64-95951ef09cbd";
        }
        // Button id's in our demo deck that we want to control
        static class DemoButtons {
            public static string SEMAPHORE_RED = "70158257-0124-4e3d-bc26-728a2d417109";
            public static string SEMAPHORE_GREEN = "cf3cd93c-e43c-4649-9f47-5441555ca7ce";
            public static string SEMAPHORE_YELLOW = "ba9a39d5-9cac-4d4e-94f0-b67fa0cbedc2";
            public static string SHIELDS_CRITICAL = "2aadc834-56f7-49e7-93d0-e047f772e7f9";
            public static string SHIELDS_PERCENT = "9ec833c2-dd31-40a8-b919-ca332fb97b51";
            public static string AMMO_COUNT = "ef59a421-e820-49e2-b223-b0efa9c95555";
            public static string WINK_BUTTON = "9ddd4517-a464-4573-89ef-d9807a2d09de";
        }

        static void Main(string[] args)
        {
            Console.Title = "MATRIC Integration Demo .Net";
            Console.WriteLine("Authorize connection in MATRIC, then enter PIN:");
            //New using integration library
            matric = new Matric.Integration.Matric(AppName);
            matric.RequestAuthorizePrompt();
            PIN = Console.ReadLine();
            matric.PIN = PIN;
            matric.OnConnectedClientsReceived += Matric_OnConnectedClientsReceived;
            matric.GetConnectedClients();
            Console.ReadLine();
        }

        private static void Matric_OnConnectedClientsReceived(object source, List<ClientInfo> clients) {
            UpdateClientsList(clients);
        }

        private static void StartDemo() {
            string GRAY = "#111111";
            string YELLOW = "yellow";
            string GREEN = "green";
            string RED = "red";
            string BLACK = "black";
            string WHITE = "white";

            matric.SetDeck(CLIENT_ID, DEMO_DECK_ID, DemoPages.SEMAPHORE);
            Thread.Sleep(800);

            List<SetButtonsVisualStateArgs> listOn = new List<SetButtonsVisualStateArgs>();
            //we will refer to the buttons by name rather then id
            listOn.Add(new SetButtonsVisualStateArgs(null, "on", buttonName: "BTN_GREEN"));
            listOn.Add(new SetButtonsVisualStateArgs(null, "on", buttonName: "BTN_YELLOW"));
            listOn.Add(new SetButtonsVisualStateArgs(null, "on", buttonName: "BTN_RED"));

            List<SetButtonsVisualStateArgs> listOff = new List<SetButtonsVisualStateArgs>();
            listOff.Add(new SetButtonsVisualStateArgs(null, "off", buttonName: "BTN_GREEN"));
            listOff.Add(new SetButtonsVisualStateArgs(null, "off", buttonName: "BTN_YELLOW"));
            listOff.Add(new SetButtonsVisualStateArgs(null, "off", buttonName: "BTN_RED"));

            matric.SetButtonsVisualState(CLIENT_ID, listOn);
            Thread.Sleep(300);
            matric.SetButtonsVisualState(CLIENT_ID, listOff);
            Thread.Sleep(300);
            matric.SetButtonsVisualState(CLIENT_ID, listOn);
            Thread.Sleep(300);
            matric.SetButtonsVisualState(CLIENT_ID, listOff);
            Thread.Sleep(300);
            matric.SetButtonsVisualState(CLIENT_ID, listOn);
            Thread.Sleep(300);
            matric.SetButtonsVisualState(CLIENT_ID, listOff);
            Thread.Sleep(300);
            matric.SetButtonsVisualState(CLIENT_ID, listOn);
            Thread.Sleep(300);
            matric.SetButtonsVisualState(CLIENT_ID, listOff);
            Thread.Sleep(300);
            matric.SetButtonsVisualState(CLIENT_ID, listOn);
            Thread.Sleep(300);
            matric.SetButtonsVisualState(CLIENT_ID, listOff);

            matric.SetButtonProperties(CLIENT_ID, DemoButtons.SEMAPHORE_GREEN, backgroundcolorOff: GREEN, buttonName: "GREEN");
            Thread.Sleep(800);
            matric.SetButtonProperties(CLIENT_ID, DemoButtons.SEMAPHORE_GREEN, backgroundcolorOff: GRAY, buttonName: "GREEN");
            matric.SetButtonProperties(CLIENT_ID, DemoButtons.SEMAPHORE_YELLOW, backgroundcolorOff: YELLOW, buttonName: "YELLOW");
            Thread.Sleep(800);
            matric.SetButtonProperties(CLIENT_ID, DemoButtons.SEMAPHORE_GREEN, backgroundcolorOff: GRAY, buttonName: "GREEN");
            matric.SetButtonProperties(CLIENT_ID, DemoButtons.SEMAPHORE_YELLOW, backgroundcolorOff: GRAY, buttonName: "YELLOW");
            matric.SetButtonProperties(CLIENT_ID, DemoButtons.SEMAPHORE_RED, backgroundcolorOff: RED, buttonName: "RED");
            Thread.Sleep(1000);

            matric.SetActivePage(CLIENT_ID, DemoPages.SHIELDS_AND_WEAPONS);
            Thread.Sleep(1500);
            //simulate battle
            int ammo = 500;
            int shields = 100;
            for (int i = 0; i < 100; i++) {
                if (shields < 30)
                {
                    matric.SetButtonProperties(CLIENT_ID, DemoButtons.SHIELDS_CRITICAL, textcolorOff: BLACK, backgroundcolorOff: RED);
                    matric.SetButtonProperties(CLIENT_ID, DemoButtons.SHIELDS_PERCENT, backgroundcolorOff: GRAY, textcolorOff: RED, text: $@"{shields}%");
                }
                else if (shields < 50)
                {
                    matric.SetButtonProperties(CLIENT_ID, DemoButtons.SHIELDS_PERCENT, backgroundcolorOff: GRAY, textcolorOff: YELLOW, text: $@"{shields}%");
                }
                else {
                    matric.SetButtonProperties(CLIENT_ID, DemoButtons.SHIELDS_PERCENT, backgroundcolorOff: GRAY, textcolorOff: GREEN, text: $@"{shields}%");
                }
                matric.SetButtonProperties(CLIENT_ID, DemoButtons.AMMO_COUNT, backgroundcolorOff: GRAY, textcolorOff: WHITE, text: $@"{ammo}");
                shields--;
                ammo = ammo -5;
                Thread.Sleep(200);
            }

            //flash critical shields
            bool criticalOn = true;
            for (int i = 0; i < 20; i++) {
                if (criticalOn)
                {
                    matric.SetButtonProperties(CLIENT_ID, DemoButtons.SHIELDS_CRITICAL, textcolorOff: GRAY, backgroundcolorOff: BLACK);
                }
                else {
                    matric.SetButtonProperties(CLIENT_ID, DemoButtons.SHIELDS_CRITICAL, textcolorOff: GRAY, backgroundcolorOff: RED);
                }
                criticalOn = !criticalOn;
                Thread.Sleep(400);
            }

            matric.SetActivePage(CLIENT_ID, DemoPages.DEMO_COMPLETE);
            Thread.Sleep(1000);
            matric.SetButtonProperties(CLIENT_ID, DemoButtons.WINK_BUTTON, text: SMILE_WINK);
            Console.WriteLine("Demo complete");

        }


        public static void UpdateClientsList(List<ClientInfo> connectedClients) {
        if (connectedClients.Count == 0)
        {
            Console.WriteLine("No connected devices found, make sure your smartphone/tablet is connected\nPress any key to exit");
            Console.ReadKey();
            Environment.Exit(0);
        }
        Console.WriteLine("Found devices:");
        foreach (ClientInfo client in connectedClients)
        {
            Console.WriteLine($@"{client.Id} {client.Name}");                
        }
            CLIENT_ID = connectedClients[0].Id;
            Console.WriteLine("Starting demo on first device");
            StartDemo();
        }
    }
}
