# KK_HSceneOptions
> Additional options and keyboard shortcuts for H in Koikatsu   


## Prerequisites  
- Afterschool DLC   
- BepInEx 5.0.1 and above



## Installation  
Download [**KK_HSceneOptions.zip** from the latest release](https://github.com/MayouKurayami/KK_HSceneOptions/releases) then extract it into your game directory (where the game exe and BepInEx folder are located). Replace old files if asked.

<br>

## Configurations  

**It is recommended to adjust the configs via the in-game plugin settings page instead of directly editing the config file.  
Press *F1* to access the plugin settings at the upper right of the screen.**  
![](https://github.com/MayouKurayami/KK_HSceneOptions/blob/master/images/HautoSets_settings.png)  


- **Auto equip sub-accessories (AutoSubAccessories)** - Auto equip sub-accessories at the start of H **(Default: Disabled)**  

- **Disable Auto Finish in Service Mode (DisableAutoPrecum)** - If enabled, animation in service modes will not be stuck in the fast precum animation when male's excitement gauge is past the 70% threshold. **(Default: Disabled)**

- **Hide shadow casted by female limbs and accessories (HideFemaleShadow)** - Hide shadow casted by female limbs and accessories. This does not affect shadows casted by the head or hair. **(Default: Disabled)**  

- **Hide shadow casted by male body (HideMaleShadow)** - Hide shadow casted by male body. Very useful for POV view or VR. **(Default: Disabled)**  

### Excitement Gauge  

- **Auto lock female gauge (LockFemaleGauge)** - Automatically lock the female excitement gauge at the start of H **(Default: Disabled)**  

- **Auto lock male gauge (LockMaleGauge)** - Automatically lock the male excitement gauge at the start of H **(Default: Disabled)**  

- **Female Excitement Gauge Maximum Value (FemaleGaugeMax)** - Female excitement gauge will not go above this value when not locked. Value ranges from 0% to 100%. **(Default: 100%)**  

- **Female Excitement Gauge Minimum Value (FemaleGaugeMin)** - Female exceitement gauge will not fall below this value when not locked. Value ranges from 0% to 100%. **(Default: 0%)**  

- **Male Excitement Gauge Maximum Value (MaleGaugeMax)** - Male excitement gauge will not go above this value when not locked. Value ranges from 0% to 100%. **(Default: 100%)**  

- **Male Excitement Gauge Minimum Value (MaleGaugeMin)** - Male excitement gauge will not fall below this value when not locked. Value ranges from 0% to 100%. **(Default: 0%)**  

### Female Speech  

- **Speech Control (SpeechControlMode)** - Configures how the plugin controls the female's speech.
  - *Default Behavior* - Disable this feature and return to vanilla behavior. **(Default)**
  - *Based on Timer* - Automatically trigger speech at set interval.
  - *Mute Idle Speech* - Prevent the girl from speaking at all at idle (she would still speak during events such as insertion).
  - *Mute All Spoken Lines* - Mute all speech other than moans.  


- **Speech Timer** - Sets the time interval at which the girl will randomly speak, from roughly 0.1 to 60 seconds. **(Default: 20)**  
 ***This option is effective only if Speech Control is set to Based on Timer***

### Force Precum  

- **Precum Timer** - When orgasm is initiated via the keyboard shortcut or in-game menu, animation will forcibly exit precum and enter orgasm after this many seconds. Set to 0 to disable this feature. **(Default: 0)**  

- **Precum Toggle** - Allow toggling throhgh precum loop when right clicking the speed control pad.  **(Default: Disabled)**  
 Toggle order: weak motion > strong motion > precum > back to weak motion


### Keyboard Shortcuts  
- **Insert After Asking Female (InsertWaitKey)** - Press this key to insert male genital after female speech **(Default: None)**  

- **Insert Without Asking (InsertWaitKey)** - Press this key to insert male genital without asking for permission **(Default: None)**  

- **Orgasm Inside (OrgasmInsideKey)** - Press this key to manually cum inside with the specified amount of time in precum **(Default: None)**  

- **Orgasm Outside (OrgasmOutsideKey)** - Press this key to manually cum outside with the specified amount of time in precum **(Default: None)**  

- **Precum Loop Toggle (OLoopKey)** - Press this key to enter/exit precum animation **(Default: None)**  

- **Spit Out (SpitKey)** - Press this key to make female spit out after blowjob **(Default: None)**  

- **Swallow (SwallowKey)** - Press this key to make female swallow after blowjob **(Default: None)**  

- **Toggle Bottom Clothes (BottomClothesToggleKey)** - Toggle through states of the bottom cloth (skirt, pants...etc) of the main female. **(Default: None)**  

- **Toggle Pantsu Stipped/Half Stripped (PantsuStripKey)** - Toggle between a fully stripped and a partially stripped pantsu. You would not be able to fully dress the pantsu with this shortcut **(Default: None)**  

- **Toggle Sub-Accessories (SubAccToggleKey)** - Toggle the display of sub-accessories **(Default: None)**  

- **Toggle Top Clothes (TopClothesToggleKey)** - Toggle through states of the top clothes of the main female, including top and bra. **(Default: None)**  

- **Trigger Speech (TriggerVoiceKey)** - Trigger a random voice line depending on the current context **(Default: None)**  


### Official VR  

- **Reset Camera At Position Change (VRResetCamera)** - Resets the camera back to the male's head when switching to a different position in official VR. **(Default: Enabled)**  

<br>

## Notes and Limitations  
- Fully supports the official VR

- Unknown compatibility with kPlug  

- The various keyboard shortcuts from this plugin can be used with a voice to keystroke software to allow hands free operation, which is especially useful in VR.  

- When manually forcing precum animation, piston speed will be fixed at max value and cannot be adjusted.


## Legal  
Copyright (C) 2020  MayouKurayami
