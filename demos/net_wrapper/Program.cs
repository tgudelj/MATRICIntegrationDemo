using System;
using System.Collections.Generic;
using System.Threading;
using Matric.Integration;

namespace IntegrationDemo {


    class Program {

        public static string DEMO_DECK_ID = CONST.DEMO_DECK_ID;
        public static string PIN = CONST.PIN;
        public static string CLIENT_ID;
        static Matric.Integration.Matric matric;

        static void Main(string[] args)
        {
            Console.Title = "MATRIC Integration Demo .Net";
            //Console.WriteLine("Authorize connection in MATRIC, then enter PIN:");
            //New using integration library
            matric = new Matric.Integration.Matric(CONST.APP_NAME, CONST.PIN, CONST.API_PORT);

 
            //matric.RequestAuthorizePrompt();
            //PIN = Console.ReadLine();
            matric.PIN = PIN;
            matric.OnConnectedClientsReceived += Matric_OnConnectedClientsReceived;
            matric.OnControlInteraction += Matric_OnControlInteraction;
            matric.GetConnectedClients();
           
            Console.ReadLine();
        }

        private static void Matric_OnControlInteraction(object data)
        {
            Console.WriteLine("Control interaction:");
            Console.WriteLine(data.ToString());
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
            listOn.Add(new SetButtonsVisualStateArgs(state:"on", buttonName: "BTN_GREEN"));
            listOn.Add(new SetButtonsVisualStateArgs(state:"on", buttonName: "BTN_YELLOW"));
            listOn.Add(new SetButtonsVisualStateArgs(state:"on", buttonName: "BTN_RED"));

            List<SetButtonsVisualStateArgs> listOff = new List<SetButtonsVisualStateArgs>();
            listOff.Add(new SetButtonsVisualStateArgs(state:"off", buttonName: "BTN_GREEN"));
            listOff.Add(new SetButtonsVisualStateArgs(state:"off", buttonName: "BTN_YELLOW"));
            listOff.Add(new SetButtonsVisualStateArgs(state:"off", buttonName: "BTN_RED"));

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


            matric.SetButtonProperties(CLIENT_ID, backgroundcolorOff: GREEN, buttonName: "BTN_GREEN");
            Thread.Sleep(800);
            matric.SetButtonProperties(CLIENT_ID, backgroundcolorOff: GRAY, buttonName: "BTN_GREEN");
            matric.SetButtonProperties(CLIENT_ID, backgroundcolorOff: YELLOW, buttonName: "BTN_YELLOW");
            Thread.Sleep(800);
            matric.SetButtonProperties(CLIENT_ID, backgroundcolorOff: GRAY, buttonName: "BTN_GREEN");
            matric.SetButtonProperties(CLIENT_ID, backgroundcolorOff: GRAY, buttonName: "BTN_YELLOW");
            matric.SetButtonProperties(CLIENT_ID, backgroundcolorOff: RED, buttonName: "BTN_RED");
            Thread.Sleep(800);

            matric.SetButtonPropertiesEx(null, new List<SetButtonPropsArgsEx> { 
                new SetButtonPropsArgsEx
                {
                    text="RED!!!", ButtonName = "BTN_RED"
                },
                new SetButtonPropsArgsEx
                {
                    text="YELLOW!!!", ButtonName = "BTN_YELLOW"
                },
                new SetButtonPropsArgsEx
                {
                    text="GREEN!!!", ButtonName = "BTN_GREEN"
                }
            });
            Thread.Sleep(2000);
            matric.SetActivePage(CLIENT_ID, DemoPages.SHIELDS_AND_WEAPONS);
            Thread.Sleep(1500);
            matric.SetControlsState(null, new List<SetControlsStateArgs> {
                    new SetControlsStateArgs
                    {
                        ControlId=null, ControlName="MULTI_POS_5", State = new { position = 5}
                    }
                    });
            //simulate battle
            int ammo = 500;
            int shields = 100;
            for (int i = 0; i < 100; i++) {
                matric.SetControlsState(null, new List<SetControlsStateArgs> { 
                    new SetControlsStateArgs
                    {
                        ControlId=null, ControlName="SHIELDS_SLIDER", State = new { value = shields}
                    }
                });
                if (shields < 30)
                {
                    matric.SetControlsState(null, new List<SetControlsStateArgs> {
                    new SetControlsStateArgs
                    {
                        ControlId=null, ControlName="MULTI_POS_5", State = new { position = 3}
                    }
                    });
                    matric.SetButtonProperties(CLIENT_ID, buttonName:"SHIELDS_CRITICAL", textcolorOff: BLACK, backgroundcolorOff: RED);
                    matric.SetButtonProperties(CLIENT_ID, buttonName:"SHIELDS_PERCENT", backgroundcolorOff: GRAY, textcolorOff: RED, text: $@"{shields}%");
                }
                else if (shields < 50)
                {
                    matric.SetControlsState(null, new List<SetControlsStateArgs> {
                    new SetControlsStateArgs
                    {
                        ControlId=null, ControlName="MULTI_POS_5", State = new { position = 4}
                    }
                    });
                    matric.SetButtonProperties(CLIENT_ID, buttonName: "SHIELDS_PERCENT", backgroundcolorOff: GRAY, textcolorOff: YELLOW, text: $@"{ shields}%");
                }
                else {
                    matric.SetButtonProperties(CLIENT_ID, buttonName: "SHIELDS_PERCENT", backgroundcolorOff: GRAY, textcolorOff: GREEN, text: $@"{ shields}%");
                }
                matric.SetButtonProperties(CLIENT_ID, buttonName: "AMMO_COUNT", backgroundcolorOff: GRAY, textcolorOff: WHITE, text: $@"{ ammo}");
                shields--;
                ammo = ammo -5;
                Thread.Sleep(100);
            }

            //flash critical shields
            matric.SetControlsState(null, new List<SetControlsStateArgs> {
                    new SetControlsStateArgs
                    {
                        ControlId=null, ControlName="MULTI_POS_5", State = new { position = 2}
                    }
                    });
            bool criticalOn = true;
            for (int i = 0; i < 10; i++) {
                if (criticalOn)
                {
                    matric.SetButtonProperties(CLIENT_ID, buttonName: "SHIELDS_CRITICAL", textcolorOff: GRAY, backgroundcolorOff: BLACK);
                }
                else {
                    matric.SetButtonProperties(CLIENT_ID, buttonName: "SHIELDS_CRITICAL", textcolorOff: GRAY, backgroundcolorOff: RED);
                }
                criticalOn = !criticalOn;
                Thread.Sleep(400);
            }

            matric.SetActivePage(CLIENT_ID, DemoPages.DEMO_COMPLETE);
            Thread.Sleep(1000);
            matric.SetButtonProperties(CLIENT_ID, buttonName: "WINK_BUTTON", text: CONST.SMILE_WINK);
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
