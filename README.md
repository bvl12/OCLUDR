# Occlusion Creator for Learning to Unilaterally Determine Re-emergence (OCLUDR) #

## To run the project ##
* Download it and open it in the Unity editor.
* Load the occlusion scene with File > Open Scene > `./Assets/Scenes/OcclusionScene.unity`

* Recording resolution can be set by adjusting the camera setting at the top left of the game view. By default it should be set to RecordingSD (640x360).
* Objects in `.prefab` form can be added to the list of objects used for data generation by placing them in the `./Assets/Resources/Prefabs` folder. Skyboxes in `.mat` form can be added by placing them in the `./Assets/Resources/Skyboxes` folder.
	* These folders are searched recursively for objects, so any files that match the extension within the folder will be used.
* All other options can be set by editing Assets/GenerateOcclusions.cs
	* `saving` enables saving of video to the ./Dataset folder, otherwise data will be generated in the engine but not saved.
	* `objectRotation` enables a random rotation for objects. Otherwise they will remain in a fixed orientation.
	* `maxClipNum` sets a maximum number of clips to save. This allows a user to generate a certain number of clips without needing to attend to the program and stop it at the right time. 
	* `updateRate` sets the object update rate (default 60fps) for motion
	* `captureRate` sets the rate at which video frames will be captured. The resulting video will be at (`updateRate/captureRate` frames per second)
	* `captureUpdates` sets the length of video clips. The total video length will be `captureUpdates*updateRate`.
	* The amplitude of the additive Perlin noise can be set with `noiseAmplitude`.
	* Object motion can be defined parametrically at the top of the `Update()` function. At present, if you wish to define your own motion, it is also necessary to modify the `GenerateOccludingConditions()` function in order to guarantee that an occlusion takes place within the specified time frame in the video.