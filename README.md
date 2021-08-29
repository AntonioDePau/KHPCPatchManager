# KHPCPatchManager

## How to apply a patch:
1. Download the [latest release](https://github.com/AntonioDePau/KHPCPatchManager/releases) or compile the binary yourself
2. Make sure you have a "resources" folder (in the same directory as the tool) with all the .txt containing the files' names (you can get one along with a release)
3. Drag a pcpatch (.kh1pcpatch, .kh2pcpatch, etc.) file onto the exe and follow the instructions

## How to extract game files:
1. Download the [latest release](https://github.com/AntonioDePau/KHPCPatchManager/releases) or compile the binary yourself
2. Make sure you have a "resources" folder (in the same directory as the tool) with all the .txt containing the files' names (you can get one along with a release)
3. Extract a PKG file by dragging its .hed onto the exe
4. A folder <pkg_name>.hed_out will be created containing all extracted files from that PKG

## How to create a patch:
1. Download the [latest release](https://github.com/AntonioDePau/KHPCPatchManager/releases) or compile the binary yourself
2. Make sure you have a "resources" folder (in the same directory as the tool) with all the .txt containing the files' names (you can get one along with a release)
3. Create a folder for your patch, let's call it "MyPatch" for example
4. Place your edited files in that folder, under <pkg_name>, and then under "original" or "remastered" (based on where the unedited files were located when you extracted them) following the initial folders and files structure.
For instance, if you wanted to edit Sora's high-poly 3D model and textures (which are located in kh2_sixth), your "MyPatch" folder would have to look like this:
```
MyPatch
└── kh2_sixth
    ├── original
    │   └── obj
    │       └── P_EX100.mdlx
    └── remastered
        └── obj
            └── P_EX100.a.us
                ├── -0.dds
                ├── -1.dds
                ├── -2.dds
                ├── -3.dds
                ├── -4.dds
                ├── -5.dds
                ├── -6.dds
                ├── -7.dds
                └── -8.dds
```
5. Drag your "MyPatch" folder onto the exe
6. A "MyPatch" with the game's patch extension will be created

## Using custom file names (hashes):
These should be automatically added to "resources/custom_filenames.txt" when patching (filenames exist in the patches, so the tool will simply write the ones that aren't natively listed)
As long as the user keeps these new entries in their "custom_filenames.txt" file, KHPCPatchManager will be able to handle these new files (extraction, re-patching, etc.).

## A huge thank you to:
- Noxalus: https://github.com/Noxalus/OpenKh/tree/feature/egs-hed-packer
- Xeeynamo and the whole OpenKH team: https://github.com/Xeeynamo/OpenKh
- DemonBoy (aka: DA) for making custom HD assets for custom MDLX files possible
- TieuLink for extensive testing and help in debugging