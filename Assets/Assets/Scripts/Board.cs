using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using Un4seen.Bass;
using System;
using System.IO;
using System.Text;

public class Board : MonoBehaviour
{
	private static int whichRemote = 0;
	private float L = 100.0f;
	public Vector4 minus = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
	public Vector4 vrednosti = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
	private float minusWeight = 0.0f;
	private float timer;

	public GameObject plane;
	private bool axis = false;
	public Material MaterialAxis;
	public bool sitting = false;

	private bool isPlaying = false;
	private bool isCalibrated = false;
	public AudioSource audioSource;
	public AudioClip[] audioClipArray;

	public float timeBetweenShots = 3000.0f;

	private bool isReady = false;
	public GameObject ready;
	public GameObject notReady;
	public GameObject calib;
	public GameObject onStart;

	public GameObject canvas;
	public GameObject canvasDebug;
	public GameObject errorCanvas;

	public TMP_Text battery;
	public TMP_Text weightDebug;
	public TMP_Text coordinates;
	public TMP_Text categoryDebug;
	public TMP_Text radioURL;
	public TMP_Text noRadio;

	private bool isDebugOn = false;

	public GameObject line;
	private Vector3 lastPosition;
	private float lastPress;
	
	private int stream;

	private int colorMode = 0;
	private int maxLines = 10;
	private float duration = 1.0f;
	private Queue<GameObject> lineQueue = new Queue<GameObject>(); // Queue to track created lines

	private int connErrCounter = 0;
	private int numRadios = 0;
	public string configId;
	public string registrationId;
	public string visualizationId;
	private string configPath;
	private string registrationPath;
	private string visualizationPath;
	private string configJSON;
	private string registrationJSON;
	private string visualizationJSON;

	[System.Serializable]
	public class Radio
	{
		public string url;
		public int tool;
	}

	[System.Serializable]
	public class Category
	{
		public int weight;
		public Radio[] radio;
	}

	[System.Serializable]
	public class CategoryList
	{
		public Category[] category;
	}

	[System.Serializable]
	public class Account
	{
		public string email;
		public string key;
	}

	[System.Serializable]
	public class Registration
	{
		public Account registration;
	}

	[System.Serializable]
	public class Visualization
	{
		public int axis;
		public int colorMode;
		public int maxLines;
		public int duration;
		public int fadeout; 
	}

	public CategoryList categories = new CategoryList();
	private Category currentCategory = new Category();
	public Registration registration = new Registration();
	public Visualization visualization = new Visualization();
	void Start()
	{
		configPath = Application.dataPath + "/StreamingAssets/" + configId + ".txt";
		registrationPath = Application.dataPath + "/StreamingAssets/" + registrationId + ".txt";
		visualizationPath = Application.dataPath + "/StreamingAssets/" + visualizationId + ".txt";
		if (!File.Exists(configPath))
        {
            // Create a file to write to.
            Debug.Log("File configJSON does not exists!");
        } else {
			configJSON = File.ReadAllText(configPath);
		}
		if (!File.Exists(configPath))
        {
            // Create a file to write to.
            Debug.Log("File registrationJSON does not exists!");
        } else {
			registrationJSON = File.ReadAllText(registrationPath);
		}
		if (!File.Exists(visualizationPath))
        {
            // Create a file to write to.
            Debug.Log("File visualizationJSON does not exists!");
        } else {
			visualizationJSON = File.ReadAllText(visualizationPath);
		}
		categories = JsonUtility.FromJson<CategoryList>(configJSON);
		registration = JsonUtility.FromJson<Registration>(registrationJSON);
		visualization = JsonUtility.FromJson<Visualization>(visualizationJSON);
		axis = visualization.axis == 1;
		colorMode = visualization.colorMode;
		maxLines = visualization.maxLines;
		duration = visualization.duration;

		if (axis)
		{
			plane.GetComponent<MeshRenderer>().material = MaterialAxis;
		}
		canvasDebug.SetActive(false);
		lastPosition = plane.transform.position;
		lastPress = 0.0f;
		canvas.SetActive(true);
		notReady.SetActive(false);
		ready.SetActive(false);
		calib.SetActive(false);
		onStart.SetActive(false);
		Debug.Log("my_DEBUG START!");
		CheckBoardStatusOnStart();

		Debug.Log("START!");
	}

	void OnEnable()
	{

		Wii.OnDiscoveryFailed += OnDiscoveryFailed;
		Wii.OnWiimoteDiscovered += OnWiimoteDiscovered;
		Wii.OnWiimoteDisconnected += OnWiimoteDisconnected;
	}

	void OnDisable()
	{
		Wii.OnDiscoveryFailed -= OnDiscoveryFailed;
		Wii.OnWiimoteDiscovered -= OnWiimoteDiscovered;
		Wii.OnWiimoteDisconnected -= OnWiimoteDisconnected;
	}

	void Update()
	{
		if (Wii.IsActive(0))
		{
			if (Wii.GetExpType(whichRemote) == 3) //balance board is in
			{
				CheckBoardStatus();
				Debug.Log("Updating");
				if (Input.GetKeyDown(KeyCode.D))
				{
					Debug.Log("Debugging on");
					isDebugOn = !isDebugOn;
					canvasDebug.SetActive(isDebugOn);
				}

				if (isDebugOn)
				{
					UpdateDebug();
				}
			}
		}
	}

	public void ExitGame()
	{
		Application.Quit();
	}

	public void CancelSearch()
	{
		Wii.StopSearch();
	}

	public void BeginSearch()
	{
		Wii.StartSearch();
		Time.timeScale = 1.0f;
	}

	public void OnDiscoveryFailed(int i)
	{
		//searching = false;
		errorCanvas.SetActive(true);
		noRadio.text =
			"Application did not discover any Wii Balance Board. Please check if Board is connected to your computer and restart the application!";
	}

	public void OnWiimoteDiscovered(int thisRemote)
	{
		Debug.Log("found this one: " + thisRemote);
		if (!Wii.IsActive(whichRemote))
			whichRemote = 0;
	}

	public void OnWiimoteDisconnected(int whichRemote)
	{
		Debug.Log("lost this one: " + whichRemote);
		errorCanvas.SetActive(true);
		noRadio.text =
			"Wii Balance Board was disconnected. Please reconnect the Board restart the application!";

	}

	private bool IsOnBoard()
	{
		return GetWeight() >= 10;
	}

	private float GetWeight()
	{
		float weight = Wii.GetTotalWeight(whichRemote);
		return weight - minusWeight;
	}

	private void CheckBoardStatusOnStart()
	{
		Debug.Log("my_DEBUG CheckBoardStatusOnStart!");

		if (IsOnBoard())
		{
			Debug.Log("my_DEBUG CheckBoardStatusOnStart! IS ON BOARD");

			isReady = false;
			notReady.SetActive(true);
		}
		else
		{
			Debug.Log("my_DEBUG CheckBoardStatusOnStart! NOT ON BOARD");
			onStart.SetActive(true);
			StartCoroutine(SetUpBoard());
		}
	}
	
	private void CheckBoardStatus()
	{
		// če je bord prazen čakamo, da oseba stopi gor
		if (isReady && !isCalibrated)
		{
			// če je oseba stopila gor, kalibriramo bord še enkrat
			// nastavimo glasbo
			// umaknemo vse nadpise
			if (IsOnBoard())
			{		
				ready.SetActive(false);
				calib.SetActive(true);

				Debug.Log("On Board: " + GetWeight());
				StartCoroutine(StepOn());
			}
			else if (minusWeight > 10)
			{
				StartCoroutine(SetUpBoard());	
			}
			// sicer ne naredimo nič -> čakamo še naprej
		} // če je osbe gor čakamo da z igranjem zaključi
		else if (isReady && isCalibrated)
		{
			// če je še vedno gor posodabljamo animacijo in glasbo
			if (IsOnBoard())
			{
				if (!isPlaying)
				{
					SatrtPlayingMusic();
					StartCoroutine(removeCalibrating());
				}
				Vector4 theBalanceBoard = Wii.GetBalanceBoard(whichRemote);
				Vector4 balanceNew = (theBalanceBoard - minus);
				// Debug.Log("vrednosti " + Math.Round(balanceNew[0]) +  " " + Math.Round(balanceNew[1]) 
				//           + " " + Math.Round(balanceNew[2]) + " " + Math.Round(balanceNew[3]));
				

				float valueFront = (float) (Math.Round((balanceNew[0] + balanceNew[1]))) / 2.0f;
				float valueBack = (float) (Math.Round((balanceNew[2] + balanceNew[3]))) / 2.0f;   
				float valueLeft = (float) (Math.Round((balanceNew[1] + balanceNew[3]))) / 2.0f;
				float valueRight = (float) (Math.Round((balanceNew[0] + balanceNew[2]))) / 2.0f;
				// Debug.Log("vrednosti Front: " + valueFront +  " Back: " + valueBack + " Left: " + valueLeft + " Right: " + valueRight);

				vrednosti = new Vector4(valueFront, valueRight, valueBack, valueLeft);
				float CoPML = (L / 2) * (((vrednosti[1]) - (vrednosti[3])) /
				                         (vrednosti[1] + vrednosti[3]));
				float CoPAP = (L / 2) * (((vrednosti[0]) - (vrednosti[2])) /
				                         (vrednosti[0] + vrednosti[2]));
				float weight = GetWeight();

				Vector3 start = new Vector3(-CoPAP * 10, weight * 7, -CoPML*10);
				// Debug.Log("Front: " + vrednosti[0] + " Back: " + vrednosti[2] + " Right: " + vrednosti[1] + " Left: " + vrednosti[3]);
				// Debug.Log("start x: " + start.x + " y: " + start.y + " z: " + start.z);

				DrawLine(start);
			}
			// sice ugasnemo muziko, vrnemo nadpis, ponovno kalibriramo
			else
			{
				isReady = false;
				StartCoroutine(StepDown());
				StopPlayingMusic();

			}
		} // če je oseba gor in nismo pripravljeni čakamo, da stopi dol
		else
		{
			if (!IsOnBoard())
			{
				StartCoroutine(SetUpBoard());
			}
		}
	}

	private void ChangeAlpha(Material mat, float alphaVal)
	{
		Color oldColor = mat.color;
		Color newColor = new Color(oldColor.r, oldColor.g, oldColor.b, alphaVal);
		mat.SetColor("_Color", newColor);

	}
	
	void DrawLine(Vector3 start)
	{
		// Create a new GameObject for the line
		GameObject lineObject = new GameObject("newLine");

		// Set the new line as a child of the specified parent GameObject
		lineObject.transform.SetParent(line.transform);
		
		LineRenderer lr = lineObject.AddComponent<LineRenderer>(); 
		
		lr.positionCount = 2;
		Material mat = new Material(Shader.Find("Sprites/Default"));
		lr.material = mat;
		lr.startWidth = 5.0f;
		lr.endWidth = 5.0f;
		lr.SetPosition(0, start);
		lr.SetPosition(1, lastPosition);

		if (colorMode == 0)
		{
			// Set the line color
			lr.startColor = Color.green; // Set the starting color of the line
			lr.endColor = Color.red;  // Set the ending color of the line
		}
		else if (colorMode == 1)
		{
			// Create a Gradient object to define the color transition
			Gradient gradient = new Gradient();
			float full = start.y + lastPress;
			float redPart = lastPress / full;
			float greenPart = start.y / full;
			gradient.SetKeys(
				new GradientColorKey[] {
					new GradientColorKey(Color.green, greenPart),  // Red at the start (0%)
					new GradientColorKey(Color.red, redPart), // Green at 30% of the line length
				},
				new GradientAlphaKey[] {
					new GradientAlphaKey(1.0f, 0.0f), // Full opacity at the start
					new GradientAlphaKey(1.0f, 1.0f)  // Full opacity at the end
				}
			);

			// Apply the gradient to the LineRenderer	
			lr.colorGradient = gradient;
		} else if (colorMode == 2)
		{
			if (start.y >= lastPress)
			{
				// Set the line color
				lr.startColor = Color.green; // Set the starting color of the line
				lr.endColor = Color.green;  // Set the ending color of the line
			}
			else
			{
				// Set the line color
				lr.startColor = Color.red; // Set the starting color of the line
				lr.endColor = Color.red;  // Set the ending color of the line
			}
		}

		// if (visualization.fadeout == 1)
		// {
		// 	float factor = 1 / (maxLines / 100);
		// 	FadeOut(factor);
		// }

		// Add the line to the queue
		lineQueue.Enqueue(lineObject);

		// If the maximum number of lines is exceeded, remove the oldest line
		if (lineQueue.Count > maxLines)
		{
			GameObject oldestLine = lineQueue.Dequeue();
			Destroy(oldestLine);
		}

		Destroy(lineObject, duration);
		// Debug.Log("end x: " + lastPosition.x + " y: " + lastPosition.y + " z: " + lastPosition.z);

		lastPosition = new Vector3(start.x, 0, start.z);
		lastPress = start.y;
	}

	private int cnt = 0;
	private void FadeOut(float factor)
	{
		Queue<GameObject> oldQueue = lineQueue;
		Queue<GameObject> newQueue = new Queue<GameObject>();
		
		// Iterate through the old queue
		while (oldQueue.Count > 0)
		{
			// Dequeue the object from oldQueue
			GameObject obj = oldQueue.Dequeue();
			LineRenderer lr = obj.GetComponent<LineRenderer>();

			Color c = lr.startColor;
			c.a = c.a - factor;
			lr.startColor = c;
			c = lr.endColor;
			c.a = c.a - factor;
			lr.endColor = c;

			Debug.Log("Fading: " + c.a);

			if (c.a > 0.1)
			{
				// Enqueue the modified object into the newQueue
				newQueue.Enqueue(obj);
			}
		}

		// Assign the new queue back to lineQueue if needed
		lineQueue = newQueue;
	}

	public class ChoosenRadio
	{
		public int category;
		public int index;
		public string url;
	}

	private ChoosenRadio currentRadio;
	public ChoosenRadio GetRadio(float userWeight)
	{
		// Find the appropriate category based on the user's weight
		Category selectedCategory = null;
		foreach (var category in categories.category)
		{
			if (userWeight <= category.weight)
			{
				selectedCategory = category;
				numRadios = category.radio.Length;
				Debug.Log("Category" + category.weight);
				break;
			}
		}

		// If no category is found, return null
		if (selectedCategory == null || selectedCategory.radio == null || selectedCategory.radio.Length == 0)
		{
			Debug.LogError("No category or radios found for the given weight.");
			return null;
		}

		// Prepare for weighted random selection
		float[] probabilities = new float[selectedCategory.radio.Length];
		for (int i = 0; i < selectedCategory.radio.Length; i++)
		{
			// Inverse of tool value for probability: Higher tool -> lower chance
			probabilities[i] = 1.0f / (selectedCategory.radio[i].tool + 1);
		}

		// Normalize probabilities
		float totalProbability = 0f;
		foreach (float p in probabilities)
		{
			totalProbability += p;
		}

		for (int i = 0; i < probabilities.Length; i++)
		{
			probabilities[i] /= totalProbability;
		}

		ChoosenRadio chosen = new ChoosenRadio();
		// Choose a radio based on weighted probabilities
		float randomValue = UnityEngine.Random.value;
		float cumulativeProbability = 0f;
		for (int i = 0; i < probabilities.Length; i++)
		{
			cumulativeProbability += probabilities[i];
			if (randomValue <= cumulativeProbability)
			{
				selectedCategory.radio[i].tool++;
				chosen.index = i;
				chosen.category = selectedCategory.weight;
				chosen.url = selectedCategory.radio[i].url;
				return chosen;
			}
		}

		// Fallback (shouldn't happen with correct probabilities)
		Debug.LogWarning("Fallback case reached in radio selection.");
		chosen.index = 0;
		chosen.category = selectedCategory.weight;
		chosen.url = selectedCategory.radio[0].url;
		return chosen;
	}

	public void SatrtPlayingMusic()
	{
		float userWeight = GetWeight();
		ChoosenRadio radio = GetRadio(userWeight);
		currentRadio = radio;
		Debug.Log(radio.url);
		Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_NET_PLAYLIST, 0);
		Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
		stream = Bass.BASS_StreamCreateURL(radio.url, 0, BASSFlag.BASS_DEFAULT,  null, IntPtr.Zero);
		
		PlayStream(radio);

		SetVolume(100);
	}
	
	private void PlayStream(ChoosenRadio radio)
	{
		if(stream != 0)
		{
			Bass.BASS_ChannelPlay(stream, false);
			Debug.Log ("BASS Starts playing");
			connErrCounter = 0;
			isPlaying = true;
		}
		else
		{
			Debug.Log ("BASS Error Code = " + Bass.BASS_ErrorGetCode());
			ApplayTool(radio);
			connErrCounter++;
			if (connErrCounter >= numRadios)
			{
				Debug.Log(connErrCounter + " " + numRadios);

				errorCanvas.SetActive(true);
				canvas.SetActive(false);
				noRadio.text =
					"Application was unable to establish any of the given radio connections in category " +
					radio.category + ". Please check the connections and restart the application!";
			}
			else
			{
				SatrtPlayingMusic();
			}
		}
	}

	private void ApplayTool(ChoosenRadio radio)
	{
		foreach (var category in categories.category)
		{
			if (radio.category == category.weight)
			{
				category.radio[radio.index].tool += 100;
			}
		}
	}

	public IEnumerator removeCalibrating()
	{
		yield return new WaitForSeconds(1.0f);
		calib.SetActive(false);
		canvas.SetActive(false);
	}

	public void StopPlayingMusic()
	{
		Debug.Log("Stop playing");
		isPlaying = false;
		Bass.BASS_ChannelStop(stream);
	}
	
	// Get the Channel Information
	public string  GetChannelInfo()
	{
		BASS_CHANNELINFO info = new BASS_CHANNELINFO();
		Bass.BASS_ChannelGetInfo(stream, info);
		return info.ToString ();
	}

	public void SetVolume(float value)
	{
		Bass.BASS_SetVolume(value);
	}

	void OnApplicationQuit()
	{
		// free the stream
		Bass.BASS_StreamFree(stream);
		// free BASS
		Bass.BASS_Free();
	}

	private IEnumerator StepOn()
	{
		Debug.Log("User stepped on");
		yield return new WaitForSeconds(0.5f);
		CalibrateBoard();
		Debug.Log("Board Calibrated");
		isCalibrated = true;
	}
	
	private IEnumerator StepDown()
	{
		yield return new WaitForSeconds(0.5f);
		CalibrateBoardOnStart();
		isCalibrated = false;
		canvas.SetActive(true);
		ready.SetActive(true);
		notReady.SetActive(false);
		calib.SetActive(false);
		onStart.SetActive(false);
		lastPosition = new Vector3(0.0f, 0.0f, 0.0f);
		while (lineQueue.Count > 0)
		{
			// FadeOut(0.01f);
			GameObject oldestLine = lineQueue.Dequeue();
			Destroy(oldestLine);
		}
	}

	private IEnumerator SetUpBoard()
	{
		Debug.Log("my_DEBUG SetUpBoard!");
		yield return new WaitForSeconds(1.0f);
		CalibrateBoardOnStart();
	}

	private void CalibrateBoardOnStart()
	{
		Debug.Log("my_DEBUG CalibrateBoardOnStart!");
		minusWeight = Wii.GetTotalWeight(whichRemote);
		Vector4 theBalanceBoard = Wii.GetBalanceBoard(whichRemote);
		minus = new Vector4(theBalanceBoard.x, theBalanceBoard.y, theBalanceBoard.z, theBalanceBoard.w);
		isReady = true;
		
		ready.SetActive(true);
		notReady.SetActive(false);
		calib.SetActive(false);
		onStart.SetActive(false);
	}
	
	private void CalibrateBoard()
	{
		Vector4 theBalanceBoard = Wii.GetBalanceBoard(whichRemote);
		// minus = new Vector4(theBalanceBoard.x, theBalanceBoard.y, theBalanceBoard.z, theBalanceBoard.w);
		// Debug.Log(minus);
	}

	private void UpdateDebug()
	{
		if (IsOnBoard())
		{
			float b = Wii.GetBattery(whichRemote);
            battery.text = "Battery: " + b + "%";
    
            float w = this.GetWeight();
            weightDebug.text = "Weight: " + Mathf.Round(w) + " kg";
            
            coordinates.text =  "x: " + vrednosti.x + " y: " + vrednosti.y + " z: " + vrednosti.z + " w: " + vrednosti.w;
            
            categoryDebug.text = "Category: " + currentRadio.category;
            
            radioURL.text = "Radio url: " + currentRadio.url;
		}
		else
		{
			coordinates.text =  "";
            
			categoryDebug.text = "Category: ";
            
			radioURL.text = "Radio url: ";
		}
		
	}
	
	void Awake()
	{
		Debug.Log("Awake");
		BeginSearch();
		BassNet.Registration(registration.registration.email, registration.registration.key);
	}

}