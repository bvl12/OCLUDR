using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class GenerateOcclusions : MonoBehaviour{
	bool saving = false;
	bool objectRotation = false;
	int counter;
	int clipnum;
	int maxClipNum = 10000;
	float updateRate = 60f;
	float captureRate = 2;
	int captureUpdates = 600;
	float noiseAmplitude = 2f;
	float frameTime;
	float time;
	float[] occludingParams;
	Vector3 occludingStartPos;
	float[] targetParams;
	Vector3 targetStartPos;
	GameObject TargetObject;
	GameObject OccludingObject;
	Camera RecordingCamera;
	RaycastHit hit;
	System.Random random;
	Vector3 occludedObjectRotationalVelocity;
	Vector3 targetObjectRotationalVelocity;
	int[] noiseStartIndices = new int[6];
	// GameObject TargetBox;
	// GameObject OccludingBox;

	List<string> AvailableObjects;
	List<string> AvailableSkyboxes;

    // Start is called before the first frame update
	void Start(){
		frameTime = 1f/updateRate;
		random = new System.Random();
		AvailableObjects = new List<string>();
		AvailableSkyboxes = new List<string>();
		ParseObjects("./Assets/Resources/Prefabs");
		ParseSkyboxes();
		RecordingCamera = Camera.main;
		counter = 1;
		clipnum = 1;
		time = 0f;
		Directory.CreateDirectory("Dataset");

		while(Directory.Exists("Dataset/" + clipnum.ToString())){
			clipnum++;
		}
		
		if(saving){
			Directory.CreateDirectory("Dataset/" + clipnum.ToString());
			Directory.CreateDirectory("Dataset/" + clipnum.ToString() + "/img");
		}


		Debug.Log(AvailableObjects.Count);
		Debug.Log(AvailableSkyboxes.Count);
		// RenderSettings.skybox = Resources.Load<Material>("Starfield Skybox/Skybox.mat") as Material;

		InitTargetObject();
		InitOccludingObject();
		GenerateOccludingConditions();
		InitNoise();
    }

    // Update is called once per frame
	void Update(){		
		// OBJECT MOTION DEFINITIONS GO HERE
			OccludingObject.transform.position = occludingStartPos + new Vector3(occludingParams[0]*time + Mathf.Sin(occludingParams[2]*time) + GetNoiseVolume(0), occludingParams[1]*time + Mathf.Sin(occludingParams[3]*time) + GetNoiseVolume(1), GetNoiseVolume(2));
			TargetObject.transform.position = targetStartPos + new Vector3(targetParams[0]*time + Mathf.Sin(targetParams[2]*time) + GetNoiseVolume(3), targetParams[1]*time + Mathf.Sin(targetParams[3]*time) + GetNoiseVolume(4), GetNoiseVolume(5));

			if(objectRotation){
				OccludingObject.transform.Rotate(occludedObjectRotationalVelocity);
				TargetObject.transform.Rotate(targetObjectRotationalVelocity);
			}
		
      if(counter % captureRate == 1){

			RecordingCamera.transform.rotation = Quaternion.Lerp(RecordingCamera.transform.rotation, GenerateCameraRotation(), 0.3f*frameTime);
			
			string imageString = "Dataset/" + clipnum.ToString() + "/img/" + (counter/2).ToString().PadLeft(5, '0') + ".png";
			if(saving)
				ScreenCapture.CaptureScreenshot(imageString);
			// Calculate Ray direction from center of object
			Bounds b = TargetObject.GetComponentsInChildren<Renderer>()[0].bounds;
			foreach (Renderer r in TargetObject.GetComponentsInChildren<Renderer>()) {
				b.Encapsulate(r.bounds);
			}
			Vector3 direction = Camera.main.transform.position - b.center; 
			// ignore the layer the target is on (31) so it doesn't generate occlusions with itself
			if(Physics.Raycast(b.center, direction, out hit, Mathf.Infinity, 31)){
				bool occludedThisFrame = (hit.collider.gameObject.name == OccludingObject.name);
				int[] boundingBox = GetBoundingBox(TargetObject);
				string gt = (counter/2).ToString() + "," + boundingBox[0].ToString() + "," + boundingBox[1].ToString() + "," + boundingBox[2].ToString() + "," + boundingBox[3].ToString() + "," + (occludedThisFrame? "1" : "0");
				if(saving){
					using (StreamWriter sw = File.AppendText("Dataset/" + clipnum.ToString() + "/groundtruth_rect.txt")){
						sw.WriteLine(gt);
					}
				}
			}
		}
		if(counter % captureUpdates == 0 && counter != 0){
			Destroy(TargetObject);
			Destroy(OccludingObject);
			InitTargetObject();
			InitOccludingObject();
			GenerateOccludingConditions();
			InitNoise();
			InitRandomSkybox();
			RecordingCamera.transform.rotation = new Quaternion(0f, 0f, 0f, 1.0f);
			clipnum++;
			if(clipnum > maxClipNum){
				saving = false;
			}
			if(saving){
				Directory.CreateDirectory("Dataset/" + clipnum.ToString());
				Directory.CreateDirectory("Dataset/" + clipnum.ToString() + "/img");
				File.WriteAllText("Dataset/" + clipnum.ToString() + "/groundtruth_rect.txt", "");
			}
			time = 0f;
			counter = 1;
			Debug.Log(clipnum);

		} else{
			time += frameTime;
			counter++;
		}
    }

	int[] GetBoundingBox(GameObject go){
		int maxX = -1;
		int minX = 1000000;
		int maxY = -1;
		int minY = 1000000;

		Bounds b = TargetObject.GetComponentsInChildren<Renderer>()[0].bounds;
			foreach (Renderer r in TargetObject.GetComponentsInChildren<Renderer>()) {
				b.Encapsulate(r.bounds);
			}


		int[] corners = {-1,1};
		foreach(int i in corners){
			foreach(int j in corners){
				foreach(int k in corners){
					Vector3 corner = new Vector3(i,j,k);
					maxX = RecordingCamera.WorldToScreenPoint(b.center + Vector3.Scale(corner,b.extents))[0] > maxX ? (int)RecordingCamera.WorldToScreenPoint(b.center + Vector3.Scale(corner,b.extents))[0] : maxX;
					minX = RecordingCamera.WorldToScreenPoint(b.center + Vector3.Scale(corner,b.extents))[0] < minX ? (int)RecordingCamera.WorldToScreenPoint(b.center + Vector3.Scale(corner,b.extents))[0] : minX;
					maxY = RecordingCamera.WorldToScreenPoint(b.center + Vector3.Scale(corner,b.extents))[1] > maxY ? (int)RecordingCamera.WorldToScreenPoint(b.center + Vector3.Scale(corner,b.extents))[1] : maxY;
					minY = RecordingCamera.WorldToScreenPoint(b.center + Vector3.Scale(corner,b.extents))[1] < minY ? (int)RecordingCamera.WorldToScreenPoint(b.center + Vector3.Scale(corner,b.extents))[1] : minY;
				}
			}
		}
		// clip bounds to frame, bounding box shouldn't be outside of the frame bounds
		maxX = (maxX < 0) ? 0 : (maxX > Screen.width) ? Screen.width : maxX;
		minX = (minX < 0) ? 0 : (minX > Screen.width) ? Screen.width : minX;
		maxY = (maxY < 0) ? 0 : (maxY > Screen.height) ? Screen.height : maxY;
		minY = (minY < 0) ? 0 : (minY > Screen.height) ? Screen.height : minY;

		int topLeftX = minX;
		int topLeftY = Screen.height - maxY;
		int width = maxX - minX;
		int height = maxY - minY;
		int[] boundingBox = {topLeftX, topLeftY, width, height};
		return boundingBox;
	}

	GameObject LoadRandomObject(){
		int index = random.Next(AvailableObjects.Count);
		return Instantiate (Resources.Load(AvailableObjects[index]), new Vector3(0,0,10), Quaternion.identity) as GameObject;
	}

	void InitOccludingObject(){
		OccludingObject = LoadRandomObject();
		Bounds b = OccludingObject.GetComponentsInChildren<Renderer>()[0].bounds;
		foreach (Renderer r in OccludingObject.GetComponentsInChildren<Renderer>()) {
			b.Encapsulate(r.bounds);
		}
		if(b.extents[0]*b.extents[1] < 0.5){
			float scale = (b.extents[0] > b.extents[1]) ? (1f/b.extents[1]) : (1f/b.extents[0]);
			OccludingObject.transform.localScale = Vector3.Scale(OccludingObject.transform.localScale, new Vector3(scale, scale, scale/2f));
		}
		b = OccludingObject.GetComponentsInChildren<Renderer>()[0].bounds;
		foreach (Renderer r in OccludingObject.GetComponentsInChildren<Renderer>()) {
			b.Encapsulate(r.bounds);
		}
		BoxCollider OccludingObjectCollider = (BoxCollider)OccludingObject.AddComponent<BoxCollider>();
		OccludingObjectCollider.size = 2 * b.extents;
		occludedObjectRotationalVelocity = new Vector3((.5f-(float)random.NextDouble())/2,(.5f-(float)random.NextDouble())/2,(.5f-(float)random.NextDouble())/2);

		float angle = (float)(random.NextDouble()*2*Mathf.PI);
		occludingStartPos = new Vector3(7f*Mathf.Cos(angle), 3f*Mathf.Sin(angle), (float)random.NextDouble()*3f);
	}

	void InitTargetObject(){
		TargetObject = LoadRandomObject();
		Vector3 objSize = GetObjectSize(TargetObject);
		if(objSize[0] * objSize[1] * objSize[2] < 2){
			TargetObject.transform.localScale = new Vector3(2f,2f,2f);
		}

		float angle = (float)(random.NextDouble()*2*Mathf.PI);
		targetStartPos = new Vector3(14f*Mathf.Cos(angle), 7f*Mathf.Sin(angle), 5f+(float)random.NextDouble()*5f);
		targetObjectRotationalVelocity = new Vector3((.5f-(float)random.NextDouble())/2,(.5f-(float)random.NextDouble())/2,(.5f-(float)random.NextDouble())/2);
		//place the target object on a layer that is ignored while raycasting because we are raycasting from the center of the object to find the camera
		TargetObject.layer = 31;
	}


	void GenerateOccludingConditions(){
		float occludingTime = Mathf.Round((2f + 2f*(float)random.NextDouble())/frameTime)*frameTime;
		
		// max angles calculated based on my selected maximum positions for the objects
		float maxVerticalAngle = 0.43662716f;
		float occludingAngleVertical = (maxVerticalAngle) - (maxVerticalAngle*2f*(float)random.NextDouble());
		
		float maxHorizontalAngle = 0.750929062f;
		float occludingAngleHorizontal = (maxHorizontalAngle) - (maxHorizontalAngle*2f*(float)random.NextDouble());
		
		float targetZ = targetStartPos[2];
		float occludingZ = occludingStartPos[2];
		//+10 on lengths because camera is positioned at z=-10
		float targetLengthVertical = (targetZ+10) / Mathf.Cos(occludingAngleVertical);
		float targetY = Mathf.Sin(occludingAngleVertical)*targetLengthVertical;
		float occludingLengthVertical = (occludingZ+10) / Mathf.Cos(occludingAngleVertical);
		float occludingY = Mathf.Sin(occludingAngleVertical)*occludingLengthVertical;

		float targetLengthHorizontal = (targetZ+10) / Mathf.Cos(occludingAngleHorizontal);
		float targetX = Mathf.Sin(occludingAngleHorizontal)*targetLengthHorizontal;
		float occludingLengthHorizontal = (occludingZ+10) / Mathf.Cos(occludingAngleHorizontal);
		float occludingX = Mathf.Sin(occludingAngleHorizontal)*occludingLengthHorizontal;


		Vector3 targetOcclusionPoint = new Vector3(targetX, targetY, targetZ);
		Vector3 occludingOcclusionPoint = new Vector3(occludingX, occludingY, occludingZ);

		float targetSinTermX = (float)random.NextDouble()*2;
		float targetSinTermY = (float)random.NextDouble()*2;
		float occludingSinTermX = (float)random.NextDouble()*2;
		float occludingSinTermY = (float)random.NextDouble()*2;

		// THESE ARE THE PARAMETERS THAT ARE USED FOR OBJECT MOTION
		// IF YOU WISH TO DEFINE YOUR OWN PARAMETRIC MOTION,
		// IT IS NECESSARY TO UPDATE THESE, AND ADJUST THEIR CALCULATION TO ENSURE OCCLUSION
		// They are set this way such that an occlusion takes place at `occludingTime`
		float[] targetParameters = {(targetOcclusionPoint[0] - targetStartPos[0] - Mathf.Sin(targetSinTermX*occludingTime))/occludingTime, (targetOcclusionPoint[1] - targetStartPos[1] - Mathf.Sin(targetSinTermY*occludingTime))/occludingTime, targetSinTermX, targetSinTermY};
		float[] occludingParameters = {(occludingOcclusionPoint[0] - occludingStartPos[0] - Mathf.Sin(occludingSinTermX*occludingTime))/occludingTime, (occludingOcclusionPoint[1] - occludingStartPos[1] - Mathf.Sin(occludingSinTermY*occludingTime))/occludingTime, occludingSinTermX, occludingSinTermY};

		targetParams = targetParameters;
		occludingParams = occludingParameters;
	}

	Quaternion GenerateCameraRotation(){
		float maxVerticalAngle = 0.43662716f;
		float maxHorizontalAngle = 0.750929062f;
		float targetAngleVertical = Mathf.Asin(TargetObject.transform.position[1]/Vector3.Distance(RecordingCamera.transform.position,TargetObject.transform.position));
		float targetAngleHorizontal =  Mathf.Asin(TargetObject.transform.position[0]/Vector3.Distance(RecordingCamera.transform.position,TargetObject.transform.position));

		if(targetAngleVertical > maxVerticalAngle || targetAngleVertical < -1*maxVerticalAngle || targetAngleHorizontal > maxHorizontalAngle || targetAngleHorizontal < -1*maxHorizontalAngle){
			return new Quaternion(-1*targetAngleVertical, targetAngleHorizontal, 0, 1);
		}

		return new Quaternion(0, 0, 0, 1);
	}

	// https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/file-system/how-to-iterate-through-a-directory-tree
	// this function recursively loops through ./Assets/Resources/ to find prefab objects and add them to the list of
	// objects that can be used as the target or the occluder
	void ParseObjects(string root){
        // Data structure to hold names of subfolders to be
        // examined for files.
        Stack<string> dirs = new Stack<string>(20);

        if (!System.IO.Directory.Exists(root))
        {
            throw new System.ArgumentException();
        }
        dirs.Push(root);

        while (dirs.Count > 0)
        {
            string currentDir = dirs.Pop();
            string[] subDirs;
            try
            {
                subDirs = System.IO.Directory.GetDirectories(currentDir);
            }
            // An UnauthorizedAccessException exception will be thrown if we do not have
            // discovery permission on a folder or file. It may or may not be acceptable
            // to ignore the exception and continue enumerating the remaining files and
            // folders. It is also possible (but unlikely) that a DirectoryNotFound exception
            // will be raised. This will happen if currentDir has been deleted by
            // another application or thread after our call to Directory.Exists. The
            // choice of which exceptions to catch depends entirely on the specific task
            // you are intending to perform and also on how much you know with certainty
            // about the systems on which this code will run.
            catch (System.UnauthorizedAccessException e)
            {
                Debug.Log(e.Message);
                continue;
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                Debug.Log(e.Message);
                continue;
            }

            string[] files = null;
            try
            {
                files = System.IO.Directory.GetFiles(currentDir);
            }

            catch (System.UnauthorizedAccessException e)
            {

                Debug.Log(e.Message);
                continue;
            }

            catch (System.IO.DirectoryNotFoundException e)
            {
                Debug.Log(e.Message);
                continue;
            }
            // Perform the required action on each file here.
            // Modify this block to perform your required task.
            foreach (string file in files)
            {
                try
                {
                    // Perform whatever action is required in your scenario.
                    System.IO.FileInfo fi = new System.IO.FileInfo(file);
                    if(fi.Extension.ToLower() ==".prefab")
											AvailableObjects.Add(file.Replace("\\", "/").Replace("./Assets/Resources/", "").Replace(fi.Extension, ""));
                }
                catch (System.IO.FileNotFoundException e)
                {
                    // If file was deleted by a separate application
                    //  or thread since the call to TraverseTree()
                    // then just continue.
                    Debug.Log(e.Message);
                    continue;
                }
            }

            // Push the subdirectories onto the stack for traversal.
            // This could also be done before handing the files.
            foreach (string str in subDirs)
                dirs.Push(str);
        }
    }

	// this function recursively loops through ./Assets/Resources/Skyboxes to find mat files to use as skyboxes for the dataset
void ParseSkyboxes(){
				string root = "./Assets/Resources/Skyboxes";
        // Data structure to hold names of subfolders to be
        // examined for files.
        Stack<string> dirs = new Stack<string>(20);

        if (!System.IO.Directory.Exists(root))
        {
            throw new System.ArgumentException();
        }
        dirs.Push(root);

        while (dirs.Count > 0)
        {
            string currentDir = dirs.Pop();
            string[] subDirs;
            try
            {
                subDirs = System.IO.Directory.GetDirectories(currentDir);
            }
            // An UnauthorizedAccessException exception will be thrown if we do not have
            // discovery permission on a folder or file. It may or may not be acceptable
            // to ignore the exception and continue enumerating the remaining files and
            // folders. It is also possible (but unlikely) that a DirectoryNotFound exception
            // will be raised. This will happen if currentDir has been deleted by
            // another application or thread after our call to Directory.Exists. The
            // choice of which exceptions to catch depends entirely on the specific task
            // you are intending to perform and also on how much you know with certainty
            // about the systems on which this code will run.
            catch (System.UnauthorizedAccessException e)
            {
                Debug.Log(e.Message);
                continue;
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                Debug.Log(e.Message);
                continue;
            }

            string[] files = null;
            try
            {
                files = System.IO.Directory.GetFiles(currentDir);
            }

            catch (System.UnauthorizedAccessException e)
            {

                Debug.Log(e.Message);
                continue;
            }

            catch (System.IO.DirectoryNotFoundException e)
            {
                Debug.Log(e.Message);
                continue;
            }
            // Perform the required action on each file here.
            // Modify this block to perform your required task.
            foreach (string file in files)
            {
                try
                {
                    // Perform whatever action is required in your scenario.
                    System.IO.FileInfo fi = new System.IO.FileInfo(file);
                    if(fi.Extension.ToLower() ==".mat")
						AvailableSkyboxes.Add(file.Replace("\\", "/").Replace("./Assets/Resources/", "").Replace(fi.Extension, ""));
                }
                catch (System.IO.FileNotFoundException e)
                {
                    // If file was deleted by a separate application
                    //  or thread since the call to TraverseTree()
                    // then just continue.
                    Debug.Log(e.Message);
                    continue;
                }
            }

            // Push the subdirectories onto the stack for traversal.
            // This could also be done before handing the files.
            foreach (string str in subDirs)
                dirs.Push(str);
        }
    }


	Vector3 GetObjectSize(GameObject go) {
        MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
 
        if (mfs.Length>0) {
            Bounds b = mfs[0].mesh.bounds;
            for (int i=1; i<mfs.Length; i++) {
                b.Encapsulate(mfs[i].mesh.bounds);
            }
            return b.extents;
        }
        else
            return new Bounds().extents;
    }


	void InitNoise(){
		for(int i = 0; i < 6; i++){
			noiseStartIndices[i] = random.Next(0,1000000);
		}
	}

	float GetNoiseVolume(int index){
		noiseStartIndices[index]+=1000;
		return (0.5f - Mathf.PerlinNoise((float)noiseStartIndices[index]/1000000,0)) * 2f * noiseAmplitude;
	}

// select a random skybox from the list of available ones
	void InitRandomSkybox(){
		int index = random.Next(AvailableSkyboxes.Count);
		RenderSettings.skybox = Resources.Load(AvailableSkyboxes[index]) as Material;

	}
}
