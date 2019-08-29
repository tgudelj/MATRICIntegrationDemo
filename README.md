# MATRIC Integration API Demo
This project is a simple demo of MATRIC integration API

MATRIC (https://matricapp.com) can be used as input device (controlling PC applications from Smartphone or tablet) by emulating keyboard and mouse, but it also provides Integration API and can be used to display data from 3rd party applications.

Integration API enables 3rd party applications to use MATRIC as an output device. Perhaps it is best illustrated by use-case example:
Let's say we have a flight simulator game, and we are using MATRIC as a virtual cockpit button board. But what if we want to reflect the game state to MATRIC, e.g. we want to indicate remaining fuel or low fuel warning. We can do that using Integration API to push changes to MATRIC deck from our flight sim, e.g. we will change the fuel button text to indicate remaining  fuel and maybe set its color to red if the fuel is low.

Integration API uses simple JSON text commands which 3rd party app sends via UDP to matric server (default port for Integration API is 50300 and it always listens on loopback address 127.0.0.1)

## How to use the demo
Prerequisites:
- Enable integration API by editing <Your user profile folder>/Documents/.matric/config.json, set "EnableIntegrationAPI" to **true**
- Import demo deck (IntegrationDemo.deck) from this repository by double clicking it.
  
## Running the integration demo
1) Start MATRIC, connect with smartphone/tablet
2) Start demo
3) MATRIC will popup a messsage similar to this one

4) Click Yes and type the PIN into demo console

5) The demo will run switching the deck on the first connected client to demo deck and dynamically changing button visuals. The demo is not ment to illustrate any programming best practices, insted it is made to be as simple and minimalistic as possible.

## Integration API commands

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
Modifies the properties of a button specified by buttonId. Note that you can change button visual properties but not it's function. This is behaviour by design. Note also that changes are not persisted, integration commands does not alter the button definition in deck on MATRIC server (PC).

Example request:

```
{
  "command":"SETBUTTONPROPS", 
  "appName":"Integration demo", 
  "appPIN":"0186", 
  "clientId":"iQyGDYxja7Zm2yuLt9MJ9Yld+aQCVXX60KV71XPpIJA=", 
  "buttonId":"cf3cd93c-e43c-4649-9f47-5441555ca7ce",
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

