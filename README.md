# BagIt Packaging Tool 2025

Date: 05/08/2025

Creating BagIt-compliant packages from a selected folder

Archiving them as ZIP or TAR files

Checksums = md5

## Features

- Drag-and-drop file/folder support
- BagIt package generation using embedded Python (bagit.py)
- Archiving with selectable format (ZIP or TAR) using SharpCompress


## Authors

- TRB1954 Ma. Pauline Mae J. Mionez
- TRB2002 Jemuel Morales 


## Documentation

Input:
- Folder file

Output:
- BagIt package folder ( md5 )
- Archived file (.zip/.tar)

Process: 
- Drag a folder into the UI
- Select archive format (ZIP/TAR)
- Press the action button (TestCommand).

The tool:
- Clones folder content into a temp output directory
- Initializes a Python runtime and runs bagit.make_bag
- Archives the BagIt package
- Cleans up temporary files

## Features

- SyncFusion control
- DevExpress.Mvvm
- Python.NET (to run bagit.py)
- SharpCompress (archive creation)
- Drag-and-Drop UI binding


## Used By

This project is used by the following Team Leaders:

- Mam Virgil Sangalang
- Mam Rene Galupo
