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
	public Material MaterialNoAxis;
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
	public TMP_Text maxLinesDebug;
	public TMP_Text durationDebug;
	public TMP_Text colorModeDebug;
	public TMP_Text axisDebug;
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
		public int toll;
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
	}

	public CategoryList categories = new CategoryList();
	private Category currentCategory = new Category();
	public Registration registration = new Registration();
	public Visualization visualization = new Visualization();
	
	private string configurningMode = "";
	void Start()
	{
		// pridobimo konfiguracijske datoteke in jih preberemo 
		configPath = Application.dataPath + "/StreamingAssets/" + configId + ".txt";
		registrationPath = Application.dataPath + "/StreamingAssets/" + registrationId + ".txt";
		visualizationPath = Application.dataPath + "/StreamingAssets/" + visualizationId + ".txt";
		if (!File.Exists(configPath))
        {
            Debug.Log("File configJSON does not exists!");
        } else {
			configJSON = File.ReadAllText(configPath);
		}
		if (!File.Exists(configPath))
        {
            Debug.Log("File registrationJSON does not exists!");
        } else {
			registrationJSON = File.ReadAllText(registrationPath);
		}
		if (!File.Exists(visualizationPath))
        {
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
			if (Wii.GetExpType(whichRemote) == 3) // preverimo ali je tip Balance Board
			{
				// preverimo koliko baterije ima board
				if (Wii.GetBattery(whichRemote) < 10)
				{
					errorCanvas.SetActive(true);
					noRadio.text =
						"Battery of the Balance Board is really low. Please change the batteries and restart the application!";
				}
				else
				{
					// preverimo status boarda
					CheckBoardStatus();
					Debug.Log("Updating " + maxLines + ", " + duration);
					// če pritisnemo tipko D se vklopi debug mode
					if (Input.GetKeyDown(KeyCode.D))
					{
						Debug.Log("Debugging on");
						isDebugOn = !isDebugOn;
						canvasDebug.SetActive(isDebugOn);
					} 
					// če pritisnemo tipko A se zamenja material na plošči
					else if (Input.GetKeyDown(KeyCode.A))
					{
						axis = !axis;
						if (axis)
						{
							plane.GetComponent<MeshRenderer>().material = MaterialAxis;
						}
						else
						{
							plane.GetComponent<MeshRenderer>().material = MaterialNoAxis;
						}
					}
					// če pritisnemo tipko C zamenjamo colorMode
					else if (Input.GetKeyDown(KeyCode.C))
					{
						colorMode = (colorMode + 1) % 3;
					}
					// če pritisnemo tipko L začnemo nastavljanje števila črt
					else if (Input.GetKeyDown(KeyCode.L))
					{
						Debug.Log("L pressed");
						configurningMode = "L";
					}
					// če pritisnemo tipko T začnemo nastavljanje trajanja črt
					else if (Input.GetKeyDown(KeyCode.T))
					{
						configurningMode = "T";
					}
					// s puščicami gor in dol povečujemo in zmanjšujemo vrednosti maxLines in duration
					else if (Input.GetKeyDown(KeyCode.UpArrow))
					{
						if (configurningMode == "T")
						{
							duration = Math.Min(duration + 1, 100);
						} 
						else if (configurningMode == "L")
						{
							maxLines = Math.Min(maxLines + 10, 1000);
						}
					}
					else if (Input.GetKeyDown(KeyCode.DownArrow))
					{
						Debug.Log("Arrow down pressed");
				
						if (configurningMode == "T")
						{
							duration = Math.Max(duration - 1, 1);
						} 
						else if (configurningMode == "L")
						{
							Debug.Log("subtracting max lines");
				
							maxLines = Math.Max(maxLines - 10, 10);
						}
					}
				}
				// če pritisnemo escape zapremo aplikacijo
				if (Input.GetKeyDown(KeyCode.Escape))
				{
					Application.Quit();
				}
				// posodobimo debug okno če smo v debug mode-u
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
			"Wii Balance Board was disconnected. Please reconnect the Board and restart the application!";

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

				Vector3 start = new Vector3(CoPAP * 10, weight * 7, -CoPML*10);
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

	void DrawLine(Vector3 start)
	{
		// Vstvarimo nov GameObject za črto
		GameObject lineObject = new GameObject("newLine");

		// Črto dodamo kot child objekt določenemu GameObjectu
		lineObject.transform.SetParent(line.transform);
		
		LineRenderer lr = lineObject.AddComponent<LineRenderer>(); 
		
		lr.positionCount = 2;
		Material mat = new Material(Shader.Find("Sprites/Default"));
		lr.material = mat;
		lr.startWidth = 5.0f;
		lr.endWidth = 5.0f;
		lr.SetPosition(0, start);
		lr.SetPosition(1, lastPosition);

		// nastavimo barvo glede na colorMode
		if (colorMode == 0)
		{
			lr.startColor = Color.green;
			lr.endColor = Color.red;
		}
		else if (colorMode == 1)
		{
			Gradient gradient = new Gradient();
			float full = start.y + lastPress;
			float redPart = lastPress / full;
			float greenPart = start.y / full;
			gradient.SetKeys(
				new GradientColorKey[] {
					new GradientColorKey(Color.green, greenPart),
					new GradientColorKey(Color.red, redPart), 
				},
				new GradientAlphaKey[] {
					new GradientAlphaKey(1.0f, 0.0f), 
					new GradientAlphaKey(1.0f, 1.0f)  
				}
			);

			lr.colorGradient = gradient;
		} else if (colorMode == 2)
		{
			if (start.y >= lastPress)
			{
				lr.startColor = Color.green; 
				lr.endColor = Color.green;
			}
			else
			{
				lr.startColor = Color.red;
				lr.endColor = Color.red; 
			}
		}

		// Dodamo črto v vrsto
		lineQueue.Enqueue(lineObject);

		// Če je vrstav napolnjena odstranimo najstarejšo črto
		if (lineQueue.Count > maxLines)
		{
			GameObject oldestLine = lineQueue.Dequeue();
			Destroy(oldestLine);
		}

		// določimo po kolikih sekundah naj se črta izbriše
		Destroy(lineObject, duration);

		lastPosition = new Vector3(start.x, 0, start.z);
		lastPress = start.y;
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
		// Glede na težo uporabnika ga umestimo v določeno kategorijo
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

		// če kategorija ni najdena vrnemo null
		if (selectedCategory == null || selectedCategory.radio == null || selectedCategory.radio.Length == 0)
		{
			Debug.LogError("No category or radios found for the given weight.");
			return null;
		}

		// Nastavimo verjetnosti, da je postaja izbrana glede na kazni
		float[] probabilities = new float[selectedCategory.radio.Length];
		for (int i = 0; i < selectedCategory.radio.Length; i++)
		{
			// Manjša kot je kazen, večja verjetnost je, da bo postaja izbrana
			probabilities[i] = 1.0f / (selectedCategory.radio[i].toll + 1);
		}

		// Normaliziramo verjetnosti
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
		// izberemo postajo glede na določene verjetnosti
		float randomValue = UnityEngine.Random.value;
		float cumulativeProbability = 0f;
		for (int i = 0; i < probabilities.Length; i++)
		{
			cumulativeProbability += probabilities[i];
			if (randomValue <= cumulativeProbability)
			{
				selectedCategory.radio[i].toll++;
				chosen.index = i;
				chosen.category = selectedCategory.weight;
				chosen.url = selectedCategory.radio[i].url;
				return chosen;
			}
		}
		
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
			ApplayToll(radio);
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

	private void ApplayToll(ChoosenRadio radio)
	{
		// dodajanje kazni radijiskim postajam, na katere se ne uspemo povezati, da je manjša verjetnost, da jih ponovno izberemo
		foreach (var category in categories.category)
		{
			if (radio.category == category.weight)
			{
				category.radio[radio.index].toll += 100;
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
		// kalibriranje boarda na začetku oziroma ko uporabnik stopi dol
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
		// kalibriranje boarda, ko stopimo na njega
		Vector4 theBalanceBoard = Wii.GetBalanceBoard(whichRemote);
	}

	private void UpdateDebug()
	{
		// posodabljanje debug okna
		if (IsOnBoard())
		{
			float b = Wii.GetBattery(whichRemote);
            battery.text = "Battery: " + b + "%";
    
            float w = this.GetWeight();
            weightDebug.text = "Weight: " + Mathf.Round(w) + " kg";
            
            coordinates.text =  "x: " + vrednosti.x + " y: " + vrednosti.y + " z: " + vrednosti.z + " w: " + vrednosti.w;

            if (currentRadio != null)
            {
	            categoryDebug.text = "Category: " + currentRadio.category;

	            radioURL.text = "Radio url: " + currentRadio.url;
            }

            maxLinesDebug.text = "Max lines: " + maxLines;
            durationDebug.text = "Duration: " + duration;
            colorModeDebug.text = "Color mode: " + colorMode;
            axisDebug.text = "Axis: " + (axis ? "Yes" : "No");
		}
		else
		{
			float b = Wii.GetBattery(whichRemote);
			battery.text = "Battery: " + b + "%";
    
			weightDebug.text = "Weight: " + 0 + " kg";
			
			coordinates.text =  "";
            
			categoryDebug.text = "Category: ";
            
			radioURL.text = "Radio url: ";

			maxLinesDebug.text = "Max lines: " + maxLines;
			durationDebug.text = "Duration: " + duration;
			colorModeDebug.text = "Color mode: " + colorMode;
			axisDebug.text = "Axis: " + (axis ? "Yes" : "No");
		}
		
	}
	
	void Awake()
	{
		Debug.Log("Awake");
		// poskusimo se povezati na Balance board
		BeginSearch();
		// povežemo se na API za predvajanje radio postaj
		BassNet.Registration(registration.registration.email, registration.registration.key);
	}

}