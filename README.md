# MATRIC Integration API Demo
This project is a simple demo of MATRIC integration API

MATRIC (https://matricapp.com) can be used as an input device (controlling PC applications from smartphone or tablet) by emulating keyboard and mouse, but can also be used to display data from 3rd party applications by utilizing Integration API.

Integration API enables 3rd party applications to use MATRIC as an output device. Perhaps it is best illustrated by use-case example:
Let's say we have a flight simulator game, and we are using MATRIC as a virtual cockpit button board. But what if we want to reflect the game state to MATRIC, e.g. we want to indicate remaining fuel or low fuel warning. We can do that using Integration API to push changes to MATRIC deck from our flight sim, e.g. we will change the fuel button text to indicate remaining  fuel and maybe set its color to red if the fuel is low.

## API Basics

Integration API uses simple JSON text commands which 3rd party app sends via UDP to matric server (default port for Integration API is 50300 and it always listens on loopback address 127.0.0.1).

## How to use the demo
Prerequisites:
- Enable integration API by editing <Your user profile folder>/Documents/.matric/config.json, set "EnableIntegrationAPI" to **true**
- Import demo deck (IntegrationDemo.deck) from this repository by double clicking it.
  
## Running the integration demo
1) Start MATRIC, connect with smartphone/tablet
2) Start demo
3) MATRIC will display a popup messsage similar to this one:

![Authorization popup](/res/authorize-app-popup.png)

4) Click Yes on the MATRIC popup and then type the PIN into demo console:

![Enter PIN in demo app console](/res/demo-enter-pin.png)

5) The demo will run switching the deck on the first connected client to demo deck and dynamically changing button visuals. The demo is not ment to illustrate any programming best practices, insted it is made to be as simple and minimalistic as possible.

## Integration API commands

### CONNECT

Triggers authorization popup on PC. User will be prompted to allow access to Integration API to 3rd party application and shown a PIN number which must be entered into 3rd party application. Typically you'd do this only once and save the PIN.

Example request:

```
{
  "command":"CONNECT",
  "appName":"Integration demo"
}
```

### GETCONNECTEDCLIENTS

Returns a list of connected clients.

Example request:

```
{
  "command":"GETCONNECTEDCLIENTS",
  "appName":"Integration demo",
  "appPIN":"0186"
}
```

Example response:

```
[
  {"Name":"Joe's Galaxy S10",
    "IP":"192.168.8.45",
    "Id":"iQyGDYxja7Zm2yuLt9MJ9Yld+aQCVXX60KV71XPpIJA=",
    "MatricVersion":2,
    "LastContact":"2019-08-29T15:28:38.1185368+02:00"}
]
```

### SETDECK

Instructs the client (specified by clientId) to load deck (specified by deckId) and (optionally) switch to page in deck (specified by pageId).

Example request:

```
{
  "command":"SETDECK", 
  "appName":"Integration demo", 
  "appPIN":"0186", 
  "clientId":"iQyGDYxja7Zm2yuLt9MJ9Yld+aQCVXX60KV71XPpIJA=", 
  "deckId":"7f18056c-7a2a-46ac-9956-662e7d0b78ec",
  "pageId":"00d9c649-f2df-405f-abfd-06d38f8626be"
}
```

### SETACTIVEPAGE

Instructs the client to switch to page specified by pageId.

Example request:

```
{
  "command":"SETACTIVEPAGE", 
  "appName":"Integration demo", 
  "appPIN":"0186", 
  "clientId":"iQyGDYxja7Zm2yuLt9MJ9Yld+aQCVXX60KV71XPpIJA=", 
  "pageId":"00d9c649-f2df-405f-abfd-06d38f8626be"
}
```

### SETBUTTONPROPS
Modifies the properties of a button specified by buttonId or buttonName. Note that you can change button visual properties but not it's function. This is behaviour by design. Note also that changes are not persisted, integration commands does not alter the button definition in deck on MATRIC server (PC).

Example request:

```
{
  "command":"SETBUTTONPROPS", 
  "appName":"Integration demo", 
  "appPIN":"0186", 
  "clientId":"iQyGDYxja7Zm2yuLt9MJ9Yld+aQCVXX60KV71XPpIJA=", 
  "buttonId":"cf3cd93c-e43c-4649-9f47-5441555ca7ce",
  "buttonName":"BIG_RED_BUTTON",
      "data":{
          "imageOff": null, 
          "imageOn":  null, 
          "textcolorOn": null, 
          "textcolorOff":null, 
          "backgroundcolorOn": null, 
          "backgroundcolorOff":"green", 
          "fontSize":null,
          "text":null
      }
}
```

### SETBUTTONPROPSEX
Modifies the properties of multiple buttons. Note that you can change button visual properties but not it's function. This is behaviour by design. Note also that changes are not persisted, integration commands does not alter the button definition in deck on MATRIC server (PC).

IMPORTANT: This method is available in version 1.22 and higher!

Example request:
```
{
     "appName": "MATRIC4DCS",
      "appPin": "9087",
    "clientId": "twQd9g6COnWeT7SPaInvcTk2xdvBW7lN8+b+a9QQRfk=",
     "command": "SETBUTTONPROPSEX",
        "data": [ {
                        "buttonId": "7eec29e4-1f59-47d2-bdd5-9b599d375fc9",
                        "imageOff": "8e7b4b9f-3e18-4009-bae1-445b1f3aa81f.png",
                    "textcolorOff": "white"
                }, {
                        "buttonName": "BIG_RED_ONE",
                        "imageOff": "8e7b4b9f-3e18-4009-bae1-445b1f3aa81f.png",
                    "textcolorOff": "white"
                }, {
                        "buttonId": "404e46c7-8b8e-48dd-89dd-276a408fcf4b",
                        "imageOff": "8e7b4b9f-3e18-4009-bae1-445b1f3aa81f.png",
                    "textcolorOff": "white"
                }, {
                        "buttonId": "d6c9836c-7986-48bc-b4a8-1cb52407bb5d",
                        "imageOff": "8e7b4b9f-3e18-4009-bae1-445b1f3aa81f.png",
                    "textcolorOff": "white"
                }, {
                        "buttonId": "2dee5490-bc3e-4c48-a6d2-144f37800993",
                        "imageOff": "8e7b4b9f-3e18-4009-bae1-445b1f3aa81f.png",
                    "textcolorOff": "white"
                } ]
}
```

### SETBUTTONSVISUALSTATE
Sets the visual state of multiple buttons defined by their ids or names. It sets predefined properties defined in MATRIC editor for "on" (pressed) and "off" (not pressed). It is easier to use then SETBUTTONPROPS and SETBUTTONPROPSEX as you do not need to define all the properties in the command but instead just set the state to on or off.

IMPORTANT: This method is available in version 1.26 and higher!

Example request:
```
{
     "appName": "MATRIC4DCS",
      "appPin": "9087",
    "clientId": "twQd9g6COnWeT7SPaInvcTk2xdvBW7lN8+b+a9QQRfk=",
     "command": "SETBUTTONSVISUALSTATE",
        "data": [ {
                        "buttonId": "7eec29e4-1f59-47d2-bdd5-9b599d375fc9",
                        "state": "on"
                }, {
                        "buttonName": "BIG_RED_ONE",
                        "state": off"
                }, {
                        "buttonId": "404e46c7-8b8e-48dd-89dd-276a408fcf4b",
                        "state": "off"
                }, {
                        "buttonId": "d6c9836c-7986-48bc-b4a8-1cb52407bb5d",
                        "state": "on"
                }]
}

```
