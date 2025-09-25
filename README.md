# SCPoseTracker
Small Test to Track Character Location and Pose Info

C# + Python system to extract in-game pose/location data from SC debug onscreen overlay,
and run SLAM to build point clouds of ship/station interiors for automated tasks.

## Components
- **MainProcess (C#)**: Captures HDMI stream from Elgato HD60, extracts pose data.
- **SLAMWorker (Python)**: Processes video frames + pose data into point cloud maps.
- **UIOverlay (C#)**: Real-time viewer for position tracking, SLAM paths, and telemetry.

## Goals
- Real-time pose tracking from video overlays
- Persistent zone-based SLAM maps
- Offline/automated route planning

## Dev Notes
- Elgato capture via OpenCV/DirectShow (C#)
- SLAM via OpenVSLAM or RTAB-Map (Python)
