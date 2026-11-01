using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using static IntegrationDemo.DemoIntegrationHelper;

namespace IntegrationDemo {


    /// <summary>Runs the demonstration on one selected device while UDP callbacks continue receiving events.</summary>
    class Program {
        public static string DEMO_DECK_ID = CONST.DEMO_DECK_ID;
        public static string PIN = CONST.PIN;
        public static string CLIENT_ID;
        public static DemoIntegrationHelper mtrx;
        private static DemoSession session;

        /// <summary>Owns the receiver and foreground animation; Ctrl+C cancels waits and releases the socket.</summary>
        static int Main(string[] args) {
            Console.Title = "MATRIC Integration Demo";
            using (var cancellation = new CancellationTokenSource()) {
                ConsoleCancelEventHandler cancel = (sender, eventArgs) => { eventArgs.Cancel = true; cancellation.Cancel(); };
                Console.CancelKeyPress += cancel;
                try {
                    var options = DemoOptions.Parse(args);
                    using (session = new DemoSession(options, cancellation.Token))
                    using (mtrx = new DemoIntegrationHelper(ParseMessage, error => Console.Error.WriteLine(error.Message), options.ApiPort, options.ListenerPort)) {
                        // PIN configuration is intentionally unchanged; no interactive authorization is introduced here.
                        mtrx.PIN = PIN;
                        CLIENT_ID = session.SelectClient(mtrx.GetConnectedClients);
                        StartDemo();
                    }
                    return 0;
                } catch (OperationCanceledException) { Console.WriteLine("Demo cancelled."); return 130; }
                catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
                finally { Console.CancelKeyPress -= cancel; }
            }
        }

        /// <summary>Demonstrates temporary appearances and state; pacing delays are not command acknowledgements.</summary>
        private static void StartDemo() {
            string GRAY = "#111111";
            string YELLOW = "yellow";
            string GREEN = "green";
            string RED = "red";
            string BLACK = "black";
            string WHITE = "white";

            mtrx.SetDeck(CLIENT_ID, DEMO_DECK_ID, DemoPages.SEMAPHORE);
            session.WaitForDeck(mtrx.GetConnectedClients, CLIENT_ID, DEMO_DECK_ID);

            List<VisualStateItem> listOn = new List<VisualStateItem>();
            // Names identify controls within the selected device's loaded deck.
            listOn.Add(new VisualStateItem(null, "on", buttonName: "BTN_GREEN"));
            listOn.Add(new VisualStateItem(null, "on", buttonName: "BTN_YELLOW"));
            listOn.Add(new VisualStateItem(null, "on", buttonName: "BTN_RED"));

            List<VisualStateItem> listOff = new List<VisualStateItem>();
            listOff.Add(new VisualStateItem(null, "off", buttonName: "BTN_GREEN"));
            listOff.Add(new VisualStateItem(null, "off", buttonName: "BTN_YELLOW"));
            listOff.Add(new VisualStateItem(null, "off", buttonName: "BTN_RED"));

            mtrx.SetButtonsVisualState(CLIENT_ID, listOn);
            session.Delay(800);
            mtrx.SetButtonsVisualState(CLIENT_ID, listOff);
            session.Delay(800);
            mtrx.SetButtonsVisualState(CLIENT_ID, listOn);
            session.Delay(800);
            mtrx.SetButtonsVisualState(CLIENT_ID, listOff);
            session.Delay(800);

            mtrx.SetButtonProperties(CLIENT_ID, "BTN_GREEN", backgroundcolorOff: GREEN);
            session.Delay(800);
            mtrx.SetButtonProperties(CLIENT_ID, "BTN_GREEN", backgroundcolorOff: GRAY);
            mtrx.SetButtonProperties(CLIENT_ID, "BTN_YELLOW", backgroundcolorOff: YELLOW);
            session.Delay(800);
            mtrx.SetButtonProperties(CLIENT_ID, "BTN_GREEN", backgroundcolorOff: GRAY);
            mtrx.SetButtonProperties(CLIENT_ID, "BTN_YELLOW", backgroundcolorOff: GRAY);
            mtrx.SetButtonProperties(CLIENT_ID, "BTN_RED", backgroundcolorOff: RED);
            session.Delay(1000);

            mtrx.SetActivePage(CLIENT_ID, DemoPages.SHIELDS_AND_WEAPONS);
            session.Delay(1500);
            //simulate battle
            int ammo = 500;
            int shields = 100;
            for (int i = 0; i < 100; i++) {
                mtrx.SetControlsState(CLIENT_ID, new List<SetControlStateItem> {
                        new SetControlStateItem(null, new { value = shields }, "SHIELDS_SLIDER")
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
                session.Delay(100);
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
                session.Delay(400);
            }

            mtrx.SetActivePage(CLIENT_ID, DemoPages.DEMO_COMPLETE);
            session.Delay(1000);
            mtrx.SetButtonProperties(CLIENT_ID, "WINK_BUTTON", text: CONST.SMILE_WINK);
            Console.WriteLine("Demo complete");


        }

        /// <summary>Decodes the legacy envelope and dispatches notifications without blocking the receiver.</summary>
        public static void ParseMessage(string json) {
            JObject obj = JObject.Parse(json);
            string messageType = obj.GetValue("MessageType").ToString();
            switch (messageType) {
                case "ClientList":
                    UpdateClientsList(obj.GetValue("MessageData").ToString());
                    break;
                case "VariablesChange":
                    Console.WriteLine("Server variables changed: " + obj.GetValue("MessageData"));
                    break;
                case "ControlInteraction":
                    OnControlInteraction(obj.GetValue("MessageData").ToString());
                    break;
            }
        }
        /// <summary>Displays control events while the foreground animation continues.</summary>
        public static void OnControlInteraction(string data)
        {
            Console.WriteLine("Control interaction:");
            Console.WriteLine(data);
        }

        /// <summary>Publishes a snapshot without starting or restarting the demo on the receive callback.</summary>
        public static void UpdateClientsList(string json) {
            session.PublishClients(JArray.Parse(json));
        }
    }
}
