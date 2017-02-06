# MaskColorChecker
A tool for Arma 3 to check the number of colors used on a surface mask.

## This tool is a WIP and may not be compatible with all setups.

Usage
========
This tool is currently intended to be used from the command line or run from a batch file. You will first want to load the terrain up in TB. Then make note of the tile size, overlap (actual overlap), tiles in row, and the number of materials per cell you selected when exporting the surface mask.

**While this program only reads your surface mask, please make a backup of it prior to running it through this program.**

Running from a batch file:
```
::example.bat
"C:\some\path\to\MaskColorChecker.exe" "C:\some\path\to\yourMask.bmp" tileSize=512 overlap=32 tilesCount=22 colorsPerTile=6 outputTiles=0
```
### tileSize
Sets the size of the tiles that TB would theoretically generate. This value can be found in TB under Mapframe properties > Samplers > Satellite/Surface (mask) tiles > Size (px).
### overlap
Sets the number of pixels the tiles overlap. This value can be found in TB under Mapframe properties > Samplers > Actual overlap (px).
### tilesCount
Sets the number of tiles in each row and column. This value can be found in TB under Mapframe properties > Samplers > Tiles in row (tile).
### colorsPerTile
Sets the number of unique colors that can be used in any given tile. If a tile has more colors than set by this value, there will be a message outputted to the console window with more information. This value can be found in TB under Mapframe properties > Processing > Imagery > Options > Export surface mask  > whichever value you selected.
### outputTiles
Determines whether the program should output the "theoretical" tile if it encounters more colors than allowed by colorsPerTile. When set to 0, the program will only give you a message in the console with more information about tiles with issues. When set to 1, the program will give that same message in the console, but will also output all of the tiles with issues to a `Bad_Tiles` folder in the same directory as your mask. For example, if your masks path was `"C:\some\path\to\yourMask.bmp"`, the tiles with issues would be outputted `"C:\some\path\to\Bad_Tiles`". *This is subject to change in future versions.*
