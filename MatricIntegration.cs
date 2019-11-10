using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IntegrationDemo {
    public class MatricIntegration {

        UdpClient udpClient;
        IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, API_PORT);
        public static int API_PORT = 50300;
        public static int UDP_LISTENER_PORT = 50301;
        private string APP_NAME = "Integration demo";

        public MatricIntegration() {
            udpClient = new UdpClient(UDP_LISTENER_PORT);
            udpClient.BeginReceive(new AsyncCallback(ReceiveCallback), null);
        }


        public void ReceiveCallback(IAsyncResult ar){
            IPEndPoint ep = new IPEndPoint(serverEP.Address, serverEP.Port);
            byte[] receiveBytes = udpClient.EndReceive(ar, ref ep);
            string receiveString = Encoding.ASCII.GetString(receiveBytes);
            Program.UpdateClientsList(receiveString);
        }

        /// <summary>
        /// Initiates the connection
        /// </summary>
        public void Connect() {
            string msg = $@"
            {{""command"":""CONNECT"", 
                ""appName"":""{APP_NAME}""}}
            ";
            UDPSend(msg);
        }

        /// <summary>
        ///Requests the list of connected MATRIC clients
        /// </summary>
        public void GetConnectedClients()
        {
            string msg = $@"
            {{""command"":""GETCONNECTEDCLIENTS"",
            ""appName"":""{APP_NAME}"",
            ""appPIN"":""{Program.PIN}""}}
            ";
            UDPSend(msg);
        }

        /// <summary>
        /// Sets various button properties
        /// </summary>
        /// <param name="clientId">Target client id</param>
        /// <param name="buttonId">Id of the button we want to change</param>
        /// <param name="text">Button text to set</param>
        /// <param name="textcolorOn">Button text color in pressed state</param>
        /// <param name="textcolorOff">Button text color in normal state</param>
        /// <param name="backgroundcolorOff">Button background color in normal state</param>
        /// <param name="backgroundcolorOn">Button background color in pressed state</param>
        /// <param name="fontSize">Button relative font size - string: small, medium, large, xlarge, xxlarge, xxxlarge</param>
        /// <param name="imageOn">Button image in pressed state</param>
        /// <param name="imageOff">Button image in normal state</param>
        public void SetButtonProperties(string clientId, string buttonId, string text = null, string textcolorOn = null, string textcolorOff = null,  
            string backgroundcolorOff = null, string backgroundcolorOn = null, string imageOn = null, string imageOff = null, string fontSize = null)
        {
            //Remark: if we do not want to change a particular property, we will send it as null
            string msg = $@"
            {{""command"":""SETBUTTONPROPS"", 
                ""appName"":""{APP_NAME}"", 
                ""appPIN"":""{Program.PIN}"", 
                ""clientId"":""{clientId}"", 
                ""buttonId"":""{buttonId}"",
                    ""data"":{{
                        ""imageOff"": { (imageOff == null ? "null" : "\"" + imageOff + "\"") }, 
                        ""imageOn"":  { (imageOn == null ? "null" : "\"" + imageOff + "\"") }, 
                        ""textcolorOn"": { (textcolorOn == null ? "null" : "\"" + textcolorOn + "\"")}, 
                        ""textcolorOff"":{ (textcolorOff == null ? "null" : "\"" + textcolorOff + "\"")}, 
                        ""backgroundcolorOn"": { (backgroundcolorOn == null ? "null" : "\"" + backgroundcolorOn + "\"")}, 
                        ""backgroundcolorOff"":{ (backgroundcolorOff == null ? "null" : "\"" + backgroundcolorOff + "\"")}, 
                        ""fontSize"":{ (fontSize == null ? "null" : "\"" + fontSize + "\"")},
                        ""text"":{ (text == null ? "null" : "\"" + text + "\"")}
                    }}
            }}";
            UDPSend(msg);
        }

        /// <summary>
        /// Sets the active page
        /// </summary>
        /// <param name="clientId">Target client id</param>
        /// <param name="pageId">Page id</param>
        public void SetActivePage(string clientId, string pageId)
        {
            string msg = $@"
            {{""command"":""SETACTIVEPAGE"", 
                ""appName"":""{APP_NAME}"", 
                ""appPIN"":""{Program.PIN}"", 
                ""clientId"":""{clientId}"", 
                ""pageId"":""{pageId}""}}
            ";
            UDPSend(msg);
        }

        /// <summary>
        /// Sets deck and optionally page
        /// </summary>
        /// <param name="clientId">Target client id</param>
        /// <param name="deckId">deck id</param>
        /// <param name="pageId">page id</param>
        public void SetDeck(string clientId, string deckId, string pageId) {
            string msg = $@"
            {{""command"":""SETDECK"", 
                ""appName"":""{APP_NAME}"", 
                ""appPIN"":""{Program.PIN}"", 
                ""clientId"":""{clientId}"", 
                ""deckId"":""{deckId}"",
                ""pageId"":""{pageId}""}}
            ";
            UDPSend(msg);
        }


        public class VisualStateItem {
            public string buttonId;
            public string state;

            public VisualStateItem(string buttonId, string state)
            {
                this.buttonId = buttonId;
                this.state = state;
            }
        }

        public void SetButtonsVisualState(string clientId, List<VisualStateItem> list) {
            string btnList = "";

            for (int i = 0; i < list.Count; i++) {
                VisualStateItem item = list[i];
                if (i != 0) {
                    btnList += ",";
                }
                btnList += $@"{{""buttonId"":""{ item.buttonId}"", ""state"":""{item.state}""}}";
            }

            string msg = $@"
            {{""command"":""SETBUTTONSVISUALSTATE"", 
                ""appName"":""{APP_NAME}"", 
                ""appPIN"":""{Program.PIN}"", 
                ""clientId"":""{clientId}"", 
                ""data"":[{btnList}]
            }}
            ";
            UDPSend(msg);
        }

        /// <summary>
        /// Sends UDP message to Matric server
        /// </summary>
        void UDPSend(string message)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            udpClient.Send(bytes, bytes.Length, serverEP);
       }
    }
}
