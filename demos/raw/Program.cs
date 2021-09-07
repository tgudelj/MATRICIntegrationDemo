using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using static IntegrationDemo.DemoIntegrationHelper;

namespace IntegrationDemo {


    class Program {
        public static string DEMO_DECK_ID = CONST.DEMO_DECK_ID;
        public static string PIN = CONST.PIN;
        public static string CLIENT_ID;
        public static DemoIntegrationHelper mtrx;
        static void Main(string[] args)
        {
            Console.Title = "MATRIC Integration Demo";
            mtrx = new DemoIntegrationHelper();
            Console.WriteLine("Authorize connection in MATRIC, then enter PIN:");
            //mtrx.Connect();
            //PIN = Console.ReadLine();
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

            List<VisualStateItem> listOn = new List<VisualStateItem>();
            //we will refer to the buttons by name rather then id
            listOn.Add(new VisualStateItem(null, "on", buttonName: "BTN_GREEN"));
            listOn.Add(new VisualStateItem(null, "on", buttonName: "BTN_YELLOW"));
            listOn.Add(new VisualStateItem(null, "on", buttonName: "BTN_RED"));

            List<VisualStateItem> listOff = new List<VisualStateItem>();
            listOff.Add(new VisualStateItem(null, "off", buttonName: "BTN_GREEN"));
            listOff.Add(new VisualStateItem(null, "off", buttonName: "BTN_YELLOW"));
            listOff.Add(new VisualStateItem(null, "off", buttonName: "BTN_RED"));

            mtrx.SetButtonsVisualState(CLIENT_ID, listOn);
            Thread.Sleep(800);
            mtrx.SetButtonsVisualState(CLIENT_ID, listOff);
            Thread.Sleep(800);
            mtrx.SetButtonsVisualState(CLIENT_ID, listOn);
            Thread.Sleep(800);
            mtrx.SetButtonsVisualState(CLIENT_ID, listOff);
            Thread.Sleep(800);          

            mtrx.SetButtonProperties(CLIENT_ID, "BTN_GREEN", backgroundcolorOff: GREEN);
            Thread.Sleep(800);
            mtrx.SetButtonProperties(CLIENT_ID, "BTN_GREEN", backgroundcolorOff: GRAY);
            mtrx.SetButtonProperties(CLIENT_ID, "BTN_YELLOW", backgroundcolorOff: YELLOW);
            Thread.Sleep(800);
            mtrx.SetButtonProperties(CLIENT_ID, "BTN_GREEN", backgroundcolorOff: GRAY);
            mtrx.SetButtonProperties(CLIENT_ID, "BTN_YELLOW", backgroundcolorOff: GRAY);
            mtrx.SetButtonProperties(CLIENT_ID, "BTN_RED", backgroundcolorOff: RED);
            Thread.Sleep(1000);

            mtrx.SetActivePage(CLIENT_ID, DemoPages.SHIELDS_AND_WEAPONS);
            Thread.Sleep(1500);
            //simulate battle
            int ammo = 500;
            int shields = 100;
            for (int i = 0; i < 100; i++) {
                mtrx.SetControlsState(CLIENT_ID, new List<SetControlStateItem> {
                        new SetControlStateItem(null, $"{{value: {shields}}}", "SHIELDS_SLIDER")
                    });
                if (shields < 30)
                {
                    mtrx.SetButtonProperties(CLIENT_ID, "SHIELDS_CRITICAL", textcolorOff: BLACK, backgroundcolorOff: RED);
                    mtrx.SetButtonProperties(CLIENT_ID, "SHIELDS_PERCENT", backgroundcolorOff: GRAY, textcolorOff: RED, text: $@"{shields}%");
                }
                else if (shields < 50)
                {
                    mtrx.SetButtonProperties(CLIENT_ID, "SHIELDS_PERCENT", backgroundcolorOff: GRAY, textcolorOff: YELLOW, text: $@"{shields}%");
                }
                else {
                    mtrx.SetButtonProperties(CLIENT_ID, "SHIELDS_PERCENT", backgroundcolorOff: GRAY, textcolorOff: GREEN, text: $@"{shields}%");
                }
                mtrx.SetButtonProperties(CLIENT_ID, "AMMO_COUNT", backgroundcolorOff: GRAY, textcolorOff: WHITE, text: $@"{ammo}");
                shields--;
                ammo = ammo -5;
                Thread.Sleep(100);
            }

            //flash critical shields
            bool criticalOn = true;
            for (int i = 0; i < 10; i++) {
                if (criticalOn)
                {
                    mtrx.SetButtonProperties(CLIENT_ID, "SHIELDS_CRITICAL", textcolorOff: GRAY, backgroundcolorOff: BLACK);
                }
                else {
                    mtrx.SetButtonProperties(CLIENT_ID, "SHIELDS_CRITICAL", textcolorOff: GRAY, backgroundcolorOff: RED);
                }
                criticalOn = !criticalOn;
                Thread.Sleep(400);
            }

            mtrx.SetActivePage(CLIENT_ID, DemoPages.DEMO_COMPLETE);
            Thread.Sleep(1000);
            mtrx.SetButtonProperties(CLIENT_ID, "WINK_BUTTON", text: CONST.SMILE_WINK);
            Console.WriteLine("Demo complete");
            //mtrx.SetDeck(CLIENT_ID, DEMO_DECK_ID, DemoPages.SEMAPHORE);

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
            CLIENT_ID = ((JObject)connectedClients[0]).GetValue("clientId").ToString();
            Console.WriteLine("Starting demo on first device");
            StartDemo();
        }
    }
}
