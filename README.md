# Spatial Audio Localization Experiment – Unity Project

This repository contains the Unity project used for a spatial audio localization experiment comparing different reproduction methods and stimulus conditions in the Ambisonics Lab at TH Köln.

The Unity application acts as the central experimental control environment. It manages the virtual scene, target/source placement, randomization, trial flow, participant interaction, tracking-based response acquisition, and the communication with the external audio playback chain.

## Project Purpose

The project was developed for a listening experiment investigating localization performance across:

- different **reproduction methods** (e.g. binaural vs. HOA),
- different **signal types**,
- different **spatial source directions**.

Unity was used to recreate the physical Ambisonics Lab setup virtually, organize the experiment workflow, and collect response data for later analysis.

## Core Functionality

The Unity project provides:

- a virtual reconstruction of the **Ambisonics Lab** loudspeaker layout,
- spatial definition of target directions for audio sources,
- semi-controlled randomization of trials,
- experiment control via keyboard input,
- tracking-based response logging,
- export of relevant trial and localization data to CSV into the folder **"Assets/ExperimentLogs"**
- communication with the external playback chain via OSC / ASIO-based interfaces.

## Experimental Logic

The experiment was designed around a simple participant workflow:

1. A trial is prepared in Unity.
2. A target source direction is selected.
3. The participant listens to the stimulus.
4. The participant indicates the perceived direction.
5. Unity logs the response, timestamps, and error measures.
6. The next trial is prepared automatically.

To reduce confounds, participants were instructed not to move during stimulus playback. Head movement during listening was logged separately and treated as a potential source of variation.

## Reproduction Chain

Unity is **not** the final rendering stage for the experiment. Instead, it functions as the control layer within a larger playback system.

The complete reproduction chain consists of:

- **Unity** for experiment logic and source control,
- **AsioAudioUnity** for ASIO-based source/output integration and source-position transmission,
- **Chataigne** for OSC/data mapping,
- **Reaper** for playback, routing, encoding, and decoding,
- external reproduction setups for **binaural** and **HOA** conditions.

Detailed configuration of the audio chain is documented separately.

## Scenes and Environment

The Unity scene represents the physical Ambisonics Lab as a functional virtual environment rather than as a visually rich simulation.

Key scene characteristics:

- spherical representation of the lab’s loudspeaker layout,
- source placement based on the real spatial reproduction setup,
- limited elevation range corresponding to the available lab setup,
- shared coordinate system for source placement and participant response evaluation.

This allows direct comparison between:

- the target direction defined in Unity,
- and the response direction derived from tracked participant orientation.

## Randomization

The project uses a semi-controlled randomization strategy.

The goal is to preserve:

- balanced representation of all experimental conditions,
- balanced distribution across azimuth quadrants,

while maintaining enough randomness to avoid predictable trial sequences and participant habituation.

In the current experiment design:

- azimuth directions are balanced across quadrants,
- azimuth values are randomized within those quadrants,
- elevation values are drawn independently from the available elevation range.

## Logged Data

Unity records the data required for later localization analysis.

Typical logged values include:

- participant ID,
- trial ID,
- signal type,
- reproduction condition / representation,
- target direction,
- response direction,
- timestamps,
- signed azimuth error,
- signed elevation error,
- overall directional error,
- head movement during listening.

The **overall directional error** is calculated as the angular distance between the response vector and the target vector. In Unity, this is implemented using `Vector3.Angle`, corresponding mathematically to the enclosed angle between normalized vectors.

## Main Scripts

The exact script set may vary depending on branch or experiment version, but core functionality is typically organized around scripts such as:

- `ExperimentController`
  - creates and manages trials,
  - handles randomization,
  - stores trial parameters,
  - triggers experiment progression.

- `TrialStateController`
  - controls the current trial state,
  - manages response and logging phases,
  - computes localization errors.

- tracking-related scripts
  - receive and interpret head orientation data,
  - provide the basis for response direction estimation.

- audio/control interface scripts
  - pass source-related information into the external playback chain,
  - trigger playback-related messages where required.

## Input and Interaction

Participant interaction was intentionally kept simple.

### Current setup
- response input via keyboard
- `Spacebar` used to start trials / confirm logging

### Planned but not used in final setup
- handheld HTC Vive controller

The controller approach proved unreliable without the corresponding HMD in the final experiment setup, so keyboard input was used instead.

## Tracking

Tracking is used to estimate participant response direction and to compare perceived direction with the actual target direction.

Important notes:

- the tracker alignment must be checked carefully before and during the experiment,
- the physical tracker setup and its virtual representation in Unity must remain aligned,
- tracking data are used for response evaluation,
- depending on the experiment version, tracking may **not** directly modify binaural rendering.

## Requirements

### Software
- **Unity**  
  Use the Unity version specified by the project files / `ProjectSettings`.
- **AsioAudioUnity**
- **Chataigne**
- **Reaper**
- additional tracker integration software if required by the experiment setup

### Hardware
- experiment PC with required audio I/O
- tracking hardware
- keyboard for participant input
- headphone and/or loudspeaker reproduction setup depending on condition
- MushRoom headphones for the binaural condition, if used in the specific experiment version

## Setup

### 1. Open the Unity project
Open the repository folder in the appropriate Unity version.

### 2. Check external dependencies
Make sure required packages and plugins are available, especially:
- tracker integration,
- ASIO/OSC-related components,
- any custom external communication scripts.

### 3. Verify lab alignment
Confirm that:
- the Unity representation matches the physical setup,
- tracker orientation is aligned,
- source directions correspond to the intended reproduction directions.

### 4. Verify external playback chain
Before starting trials, ensure that:
- Chataigne is running and correctly mapped,
- Reaper is ready,
- the intended output condition is selected,
- OSC / ASIO communication is active.

## Running an Experiment

1. Launch the required external software.
2. Open the correct Unity scene.
3. Enter or verify participant/trial settings.
4. Check tracker alignment.
5. Start the experiment.
6. Monitor logging and playback state during the session.
7. Save/export the resulting CSV data.

## Troubleshooting

### No playback is triggered
- Verify Chataigne and Reaper are running.
- Check OSC communication.
- Check track names / message routing.
- Verify that the correct condition is active.

### Tracking seems misaligned
- Recalibrate before starting.
- Check physical placement of the tracker.
- Verify virtual tracker orientation in Unity.
- Re-check alignment during the experiment if necessary.

### Logged directions seem incorrect
- Verify coordinate system assumptions.
- Check azimuth/elevation conversion.
- Confirm quadrant logic and target assignment.
- Inspect the error calculation scripts.

### Participant input is not registered
- Confirm the correct input window is focused.
- Verify keyboard bindings.
- Check whether trial state changes are blocked by another condition.

### CSV files are missing or incomplete
- Verify the output path.
- Check file permissions.
- Confirm that the logging method is called at the correct stage in the trial flow.

## Known Limitations

- The project depends on an external playback ecosystem and is therefore not fully self-contained.
- Accurate operation requires careful alignment between physical and virtual setups.
- Results are sensitive to tracking accuracy and experiment configuration.