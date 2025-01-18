# Setup
Place the files in C:\Tools</br>
make sure you got NPM installed : https://nodejs.org/en/download/</br>
Run C:\Tools\RunMeFirst.bat

# Step 1
Download all the swf's with the download tool : C:\Tools\DownloadHabbo\Habbo_Downloader.exe

Download all the required assets

```cmd
clothes
download furnidata 
download productdata
download furniture
download variables
download texts
download icons
```
This will download all that is required.

# Step 2
run the "c:\Tools\run.bat" this will confurt all the SWF / Config files to .nitro and .json for the config.

We already place the default and extra effects in "C:\Tools\asset_default" so this will be done automaticly.

When done all the files will be in : C:\tools\Convert\assets

And that's it you are ready to Rock !
* Please note this is for the default furniture only, all costums your on your own !

# Manualy start the converter converter

Installation :

* npm i yarn -g
* yarn install
* yarn build

Use the Converter:

- yarn start - downloads swf as usual
- yarn start:bundle - bundles files from assets/extracted/${ type } to .nitro 
- yarn start:extract - extracts .nitro from assets/extract/${ type } to the files within the .nitro so you can edit them 
- yarn start:convert-swf - converts swf files from assets/bundle/${ type } to .nitro without needing furnidata file

# What is new ?
Blazing fast badge downloader for habbo badges.</br>
Improved habbo classes for downloading all the rest</br>
Added .nitro support so a easy way to download all furni / clothes from every retro (instruction in the config.ini what to do!)</br>
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
  
The SQL Furni generator, this will generate all the SQL's for you.</br>
- Step 1 => just place all .nitro files into the Generate/Furniture that you want in the SQL
- Step 2 => place the FurnitureData.json in the Generate/Furnidata
- Last step : in the download tool run: Generate SQL
So more Copy & Paste and way more easyer to combine stuff from other resources!


# Credits
- The whole habbo community.
- Credits for the Converter : Nitro Team https://discord.gg/yCXcMqrT
- AtlasOmega for "the Among Us effects" : Enable 880 until 903
- Leet for the enables : Enable 500 until 688
