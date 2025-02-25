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
Download all original HAbbo assets/br>
Added .nitro support so a easy way to download all furni / clothes / pets from every retro (instruction in the config.ini what to do!)</br>
Multi Merge option for easy backtrack of all your custom and easy intergrate all your needs for Furni and clothes (i have added examples in the merge directory)</br>
A new written Compile and Decompile for all .nitro files</br>
The Compiler works as followed when you want to edit .nitro files this are the steps ( i used the same logic as Laynester was doing ).</br>
- Step 1 : place your .nitro files in the /Compiler/extract <= This can be Furni / Pets / Clothes / Effects
- Step 2: In the download tool run the command : NitroFurniextract
- Step 3: Now all the files are DeCompiled and ready to be edit in the /Compiler/extracted Directory
- Step 4: When you are done Editing your files place all the directories you want to compile from the /Compiler/compile
- Step 5: In the download tool run the command: NitroFurnicompile
- And the last step: All your new .nitro files will be in /Compiler/compiled

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
Here you can confurt all the above to .nitro files so they can be used for nitro based hotels

Database tools</br>
- show Database version
- Database Optimize
- Database fix the the offer_id.
- Database Fix Settings lay / walk on / sit in the database.
- Datavase Fixing Sprite_ID in items_base

For the custom Effects they are in the Addons\Custom Effects folder, please read the README.md how to import this, ofcourse you can customize if you want !

# Credits
- The whole habbo community.
- Credits for the Converter : Nitro Team https://discord.gg/yCXcMqrT (this is only used for the Pets)
- AtlasOmega for "the Among Us effects" : Enable 880 until 903
- Leet for the enables : Enable 500 until 688
