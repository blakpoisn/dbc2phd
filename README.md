# dbc2phd
Create CAN SAE J1939 Protocol database JSON configuration file for Parker Hannifin PHD Displays directly from a Vector DBC file.
### Helful Links
- [Parker PHD Displays](https://ph.parker.com/us/17616/en/phd) 
- [Vector CANdb++ Editor](https://www.vector.com/int/en/download/?tx_vectorproducts_productdownloaddetail%5Baction%5D=show&tx_vectorproducts_productdownloaddetail%5Bcontroller%5D=Productdownload&tx_vectorproducts_productdownloaddetail%5Bdownload%5D=54817&cHash=8adc056c8357025d3610a12fb823c59d)
## How to Use
### Directions
- Select the DBC file you wish to generate JSON from.
- Make sure to use the compatible DBC file. If not, use the "Make DBC Compatible" button to do so.
- Use "Open in Editor" button to open the DBC in Vector CANdb++ Editor. (Make sure the editor is installed in your system and *.dbc file is associated with it.)
- For a valid DBC file click the "Parse DBC" button to decode the DBC file.
- Select the node for which you want to create JSON configuration.
- Click "JSON Path" button to create or select the JSON file path.
- Click "Generate" button to generate the JSON file.
### Command
    dbc2phd [dbc_file_path] [node] [JSON_file_path]

- **dbc_file_path** : Location of the dbc file you wish to generate JSON for. Needs to be dbc2phd compatible.
- **node** : CAN node name of the PHD Display.
- **JSON_file_path** : Location for the generated JSON file.
