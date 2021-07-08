# dbc2phd
Create CAN SAE J1939 Protocol database JSON configuration file for Parker Hannifin PHD Displays directly from a Vector DBC file.
### Helful Links
- [Parker PHD Displays](https://ph.parker.com/us/17616/en/phd)
- [PHD API Documentation](https://www.parker.com/Literature/Electronic%20Controls%20Division/Literature%20files/PHD_API_Reference_MSG33-5021-M3.pdf)
- [Vector CANdb++ Editor](https://www.vector.com/int/en/download/?tx_vectorproducts_productdownloaddetail%5Baction%5D=show&tx_vectorproducts_productdownloaddetail%5Bcontroller%5D=Productdownload&tx_vectorproducts_productdownloaddetail%5Bdownload%5D=54817&cHash=8adc056c8357025d3610a12fb823c59d)
## How to Use
### Directions
- Select the DBC file you wish to generate JSON from.
- Make sure to use the compatible DBC file. If not, use the "Make DBC Compatible" button to do so.
- Use "Open in Editor" button to open the DBC in Vector CANdb++ Editor. (Make sure the editor is installed in your system and *.dbc file is associated with it.)
- For a valid DBC file click the "Parse DBC" button to decode the DBC file.
- Select the node for which you want to create JSON configuration.
- Click "Save JSON" button to create JSON configuration file.
- Click "Re-Create" button to generate the JSON file code again.
### Command
    dbc2phd [dbc_file_path] [node] [JSON_file_path]

- **dbc_file_path** : Location of the dbc file you wish to generate JSON for. Needs to be dbc2phd compatible.
- **node** : CAN node name of the PHD Display.
- **JSON_file_path** : Location for the generated JSON file.
- If all above arguments are passed while calling the app, it will generate the JSON file and exit. Thus, can be used as single shot JSON configuration generator via batch file.
### DBC Attributes - Compatible
| Section | Attribute | Description | 
|--|--|--|
| Node | NmStationAddress | Source Address [SA] of the node |
| Node | NmJ1939AAC | J1939 Name field: Arbitrary Address Capable |
| Node | NmJ1939IdentityNumber | J1939 Name field: Identity Number |
| Node | NmJ1939IndustryGroup | J1939 Name field: Industry Group |
| Node | NmJ1939SystemInstance | J1939 Name field: System Instance |
| Node | NmJ1939System | J1939 Name field: System |
| Node | NmJ1939Function | J1939 Name field: Function |
| Node | NmJ1939FunctionInstance | J1939 Name field: Function Instance |
| Node | NmJ1939ManufacturerCode | J1939 Name field: Manufacturer Code |
| Node | NmJ1939ECUInstance | J1939 Name field: ECU Instance |
| Node | PHD_DM1_configPath | DM1 configuration file path in Application file structure |
| Message | PHD_ignoreSourceAddress | Ignore Source Address of message |
| Message | PHD_notifyStale | Enable stale notification |
| Message | PHD_staleTimeoutPeriod | Timeout period for stale notification |
| Message | PHD_rateLimit | Limit rate of event trigger due to this message |
| Message | PHD_ignoreDuplicate | Ignore duplicate value event triggers |
| Message | GenMsgSendType | Send type for the message |
| Message | GenMsgCycleTime | Rate at which message is sent, if message is periodic |
| Signal | PHD_SignalDataType | Signal type (DBC signal type does not cover string type) |
| Signal | PHD_stringLength | String length in characters(bytes) in case of string type |
