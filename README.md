# AR-Remote-Surgical-Assistant

This AR Remote Surgical Assistant is a mixed-reality application enabling surgeons to receive real-time feedback and guidance from assistants located remotely. The system integrates a Magic Leap 2 AR headset for the surgeon and a desktop-based application for the assistant, connected through WebRTC for video, audio, and data communication. Key functionalities include live video streaming, two-way audio, annotated visuals, and 2D/3D projections of shared reference models and images. The project prioritizes minimal interference with surgical tasks while fostering collaboration and training opportunities without the need for co-location.

## Team Members
* Luca Sichi
* Louis Niederlöhner
* Siddharth Menon
* Ayshé Opan

### Supervised by:
* Matthias Rüger (MD, Zurich)
* Javier Narbona Cárceles (MD PhD, Madrid)

## The Code

The code for the Surgeon/Magic Leap can be found in the folder "AR_surgical_assistant"

The code for the assistant can be found in the folder "AR-Assistant_Client"

Note that for the assistant FMOD for unity has to be installed. The installation can be found under "https://www.fmod.com/download#fmodforunity"

The python server can be found at "AR-Remote-Surgical-Assistant/AR_surgical_assistant/Server/server.py". We used python version 3.9 to run the server.
