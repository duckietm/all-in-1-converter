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

# Custom clothes / furni etc.
In the Habbo_Downloader.exe.config you can change all the variables to set it to other languages, change download url's etc. etc.
Please always ask the owner of an hotel to use his or here assests !!!

# Credits
- undeƒined for contributing
- Credits for the Converter : Nitro Team https://discord.gg/yCXcMqrT
- AtlasOmega for "the Among Us effects" : Enable 880 until 903
- Leet for the enables : Enable 500 until 688
