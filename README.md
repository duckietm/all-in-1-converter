# Setup
Place the files where you like</br>
make sure you got
- .NET SDK : https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.406-windows-x64-installer</br>
- Latest NodeJS: https://nodejs.org/en/download


# Step 1
Download all the swf's with the download tool : C:\Tools\DownloadHabbo\Habbo_Downloader.exe

Download all the required assets

```cmd
Go to : C:\Repo\all-in-1-converter\DownloadHabbo\Compiled
Check the config.ini to make sure you have selected the right language
Run the : Habbo Downloader.exe
Select option : 1
type : Download All
```
This will download all that is required.

# What is new ?
Blazing fast badge downloader for habbo badges.</br>
Download all original Habbo assets</br>
Added .nitro support so a easy way to download all furni / clothes / pets from every retro (instruction in the config.ini what to do!)</br>
Multi Merge option for easy backtrack of all your custom and easy intergrate all your needs for Furni and clothes (i have added examples in the merge directory)</br>
A new written Compile and Decompile for all .nitro files</br>

Also i changed the SQL-Generator to load variables from the .nitro furni
- width
- length
- height
- interaction count
  
The SQL Furni generator, this will generate all the SQL's for you.</br>
- Step 1 => just place all .nitro or .swf files into the Generate/Furniture that you want in the SQL
- Step 2 => place the FurnitureData.json in the Generate/Furnidata
- Last step : in the download tool run the Generate SQL (option 4)
So more Copy & Paste and way more easyer to combine stuff from other resources!

The SWF to Nitro generator</br>
- Clothes
- Furniture
- Pets
- Effects
Here you can confurt all the above to .nitro files so they can be used for nitro based hotels

Database tools</br>
- show Database version
- Database Optimize
- Database fix the the offer_id.
- Database Fix Settings lay / walk on / sit in the database.
- Datavase Fixing Sprite_ID in items_base

For the custom Effects they are in the Addons\Custom Effects folder, please read the README.md how to import this, ofcourse you can customize if you want !

# Instructions

There are 4 main functions:
* Download all Default habbo Assets
* Download furni / clothes from other retro's
* Tools ==> Merge / Generate SQL / Decompile Nitro / Compile Nitro / All SWF to Nitro
* Database Tools

Database ==>
* Fix Sprite_ID and Item_IDS in the items_base from the JSON
* Fix Sit / Lay / Walk in the items_base with the settings from the json
* Fix the Offer_ID in the database from the JSON
* Optimize your database
* And show some info

There will be folders created :

Database / Variables ==> place here in the variables the FurnitureData.json you want to use to fix your database


Generate has 3 folders:
Furnidata ==> place here in the FurnitureData.json you want to use.

Furniture ==> place here furniture you want to add to the SQL file this can be .nitro or .swf it will read all off them</br>
And the Output SQL here will be the SQL files you generate, it will make on timestamp so you can keep a record.</br>
The only thing you need to provide :</br>
Enter the starting ID for items_base and catalog_items: 9000 <== Example of the starting id of the Items_Base (you can see the last entry by : 
select * from items_base order by id desc
)</br>
Enter the Catalog_Page ID for catalog_items: 9000 <== this is the catalog_page id you want the furni in</br>
Habbo Default is straight forward files are the JSON / Text if you use flash</br>

Nitrocompile is also very simple:</br>
compile  <== Place the JSON and Image file here and it will create the .nitro (example silo.json silo.png</br>
compiled <== here will the output of the .niro from compile</br>
extract <== place here the .nitro you want to extract</br>
extracted <== here will be the output off the nitro so you can alter it etc.</br>

SWFCompiler this was the most work for me 🙂 here you can create SWF to Nitro file's</br>
there are 4 folders for the output</br>
- clothes
- effects
- furniture
- pets

And one folder if you want to import SWF, just place the SWF in the right folder : funri / pet etc.  and as you will see i already added the latest Pets and also all the default + custom effects so you do not have to search</br>

when you run one of the SWF to Nitro it will ask you for clothes / funri :</br>
Do you want (H) Hof_Furni or (I) Imported clothes? (Default is H):  <== here you can choose to use the default habbo folder or the custom import folder.</br></br>

# Credits
- The whole habbo community.
- Credits for the Converter : Nitro Team https://discord.gg/yCXcMqrT (this is only used for the Pets)
- AtlasOmega for "the Among Us effects" : Enable 880 until 903
- Leet for the enables : Enable 500 until 688

# Want to help !
Discord: https://discord.gg/txfNucJv

Please beware all is Free so don't get scammed !!
