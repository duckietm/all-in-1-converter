@echo off
mkdir C:\Tools\Convert\assets\Furni_Icons
copy C:\Tools\DownloadHabbo\hof_furni\icons\*.* C:\Tools\Convert\assets\Furni_Icons\*.*
cd c:\Tools\Convert
yarn start
explorer C:\Tools\Convert\assets