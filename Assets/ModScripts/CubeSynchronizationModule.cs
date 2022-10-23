using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CubeSynchronization;
using RNG = UnityEngine.Random;

public partial class CubeSynchronizationModule : KtaneModule
{
	public Transform FlashingObjects;
	public Transform Buttons;
	public GameObject RegainButton;
	public TextMesh RNum;
	public TextMesh FaceNum;
	public TextMesh Move;
	public TextMesh IDNum;
	public TextMesh DisplayText;

	private static List<Cube> Cubes = new List<Cube>();
	private static string RawNumber = "";
	private static int FacesAdded = 0;

	private List<Cube> AllCubes = new List<Cube>();

	private float FacesPerStageRaw = 0f;
	private int FacesPerStage;

	private float MovementTimesRaw;

	private int QueuedStages;
	private int CompletedStages;
	
	private string FinalNumber;
	private string Input;
	private bool solved = false;
	private static int ReadyModules;
	private bool ReadyToSolve = false;
	private int CurrentCubeCalc;
	private int CurrentCube;

	private static int StageSolves;

	private static List<CubeSynchronizationModule> AllModules = new List<CubeSynchronizationModule>();
	
	private readonly int[] UseMovements = new int[]
	{
		-2,
		-1,
		1,
		2,
		3,
		-3
	};

	private readonly int[] Opposites = new int[]
	{
		5,
		2,
		1,
		4,
		3,
		0
	};

	private readonly Dictionary<MovementType, string> MovementCharacters = new Dictionary<MovementType, string>()
	{
		{MovementType.Up, "↑"},
		{MovementType.Down, "↓"},
		{MovementType.Left, "←"},
		{MovementType.Right, "→"}
	};

	private Dictionary<int, int> CubeIndexes = new Dictionary<int, int>()
	{
		{0, 0}
	};
	
	[HideInInspector]
	public int GreenNumber;

	protected override void Awake()
	{
		base.Awake();
		AllModules.Clear();
	}

	protected override void Start()
	{
		base.Start();
		GetIgnoredModules("Cube Synchronization", new string[] {"Cube Synchronization"});
		RegainButton.SetActive(false);
		BombModule.OnActivate += () =>
		{
			Cubes.Clear();
			StageSolves = 0;
			RawNumber = "";
			FacesAdded = 0;
			FacesPerStageRaw += TwitchPlaysScore < 0 ? 0 : TwitchPlaysScore;
			FacesPerStageRaw += TweaksScore < 0 ? 0 : (float)TweaksScore;
			if (FacesPerStageRaw <= 0f) FacesPerStageRaw = 4f;
			FacesPerStage = Mathf.CeilToInt(FacesPerStageRaw)%10;
			MovementTimesRaw += TwitchPlaysPPM < 0 ? 0 : TwitchPlaysPPM;
			MovementTimesRaw += TweaksPPM < 0 ? 0 : (float) TweaksPPM;
			if(MovementTimesRaw<=0f) MovementTimesRaw=3f;
			GreenNumber = (ModuleID % MaxID)+1;
			IDNum.text = GreenNumber.ToString();
			AllModules.Add(this);
			AllModules = AllModules.OrderBy(module => module.GreenNumber).ToList();
			Logger(String.Format("TwitchPlaysBase: {0}, TweaksBase: {1}, TwitchPlaysPPM: {2}, TweaksPPM: {3}", TwitchPlaysScore, TweaksScore, TwitchPlaysPPM, TweaksPPM));
			Logger("Green number: "+GreenNumber);
			Logger("Faces per stage: "+FacesPerStage);
			if(BombInfo.GetSolvableModuleNames().All(x => IgnoredModules.Contains(x)))
			{
				solved = true;
				BombModule.HandlePass();
			}
			RegainButton.GetComponent<KMSelectable>().OnInteract += () =>
			{
				RegainButton.SetActive(false);
				Logger(String.Format("Trying to show cube #{0}; {1}", CurrentCube, AllCubes.Count));
				Cube cube = AllCubes[CurrentCube];
				foreach (var module in AllModules) module.ShowDisplay(false);
				int counter = 0;
				foreach (int face in cube.ShowOrder)
				{
					var module = AllModules[counter++ % MaxID];
					module.QueueRoutine(module.ShowCompletedFace(cube.Faces[face], face+1));
				}
				foreach (var module in AllModules) module.QueueRoutine(module.ResetDisplay());
				return false;
			};
		};
		OnNewStage += (ModuleName, ReadyToSolve) =>
		{
			if (GreenNumber == 1)
			{
				foreach(var module in AllModules) module.HandleNewStage(ReadyToSolve);
			}
		};
	}

	public void QueueRoutine(IEnumerator routine)
	{
		QueueRoutines(routine);
	}

	public void ShowDisplay(bool disable)
	{
		FlashingObjects.gameObject.SetActive(!disable);
		DisplayText.transform.parent.gameObject.SetActive(disable);
		Buttons.gameObject.SetActive(disable);
	}
	public IEnumerator ShowCompletedFace(CubeFace face, int FaceNumber)
	{
		RNum.text = face.Number.ToString();
		Move.text = MovementCharacters[face.UseSecondary ? face.SecondaryBackMovement : face.BackMovement];
		FaceNum.text = FaceNumber.ToString();
		StartCoroutine(ChangeColor(RNum.color, c => RNum.color = c));
		StartCoroutine(ChangeColor(FaceNum.color, c => FaceNum.color = c));
		StartCoroutine(ChangeColor(Move.color, c => Move.color = c));
		if (face.UseSecondary)
		{
			yield return RotateModule(4.3f, face.BackMovement);
			yield return new WaitForSeconds(.3f);
		}
		else yield return new WaitForSeconds(4.5f);
	}

	public IEnumerator ResetDisplay()
	{
		ShowDisplay(true);
		yield break;
	}

	public void HandleNewStage(bool ReadyToSolve)
	{
		WatchSolves = !ReadyToSolve;
		QueueRoutines(true, QueueStage(ReadyToSolve));
	}


	private IEnumerator QueueStage(bool ReadyToSolve)
	{
		for (int i = 0; i < FacesPerStage; i++) yield return null;
		QueueRoutines(true, ShowStage(ReadyToSolve));
	}
	
	
	private void ShowButtons()
	{
		foreach (Transform child in Buttons) child.gameObject.SetActive(true);
	}
	
	public bool SubmitDigit(string Digit)
	{
		if (solved) return true;
		string TempInput = Input + Digit;
		if (!FinalNumber.StartsWith(TempInput))
		{
			if(CurrentCube<AllCubes.Count) RegainButton.SetActive(true);
			Logger("Invalid digit entered: " + Digit);
			BombModule.HandleStrike();
			return false;
		}
		RegainButton.SetActive(false);
		Input = TempInput;
		if (CubeIndexes.ContainsKey(Input.Length))
		{
			CurrentCube = CubeIndexes[Input.Length];
			Logger(String.Format("Advancing to {0}.", CurrentCube<AllCubes.Count ? "cube #"+(CurrentCube + 1) : "the green number"));
		}
		DisplayText.text = Digit;
		ShowButtons();
		if (Input == FinalNumber)
		{
			Logger("All correct digits are entered!");
			BombModule.HandlePass();
			solved = true;
		}
		return true;
	}

	private IEnumerator ShowStage(bool ReadyToSolve)
	{
		if (FacesAdded == FacesPerStage * MaxID)
		{
			Debug.Log("Clearing");
			RawNumber += CalculateStage(++StageSolves);
			Cubes.Clear();
			FacesAdded = 0;
		}
		if (Cubes.Count == 0) Cubes.Add(new Cube());
		for (int i = 0; i < FacesPerStage; i++)
		{
			var _CurrentCube = Cubes[Cubes.Count - 1];
			int RNumber = RNG.Range(30, 51);
			var Empties = _CurrentCube.Empties;
			if (Empties.Length == 0)
			{
				Cubes.Add(new Cube());
				_CurrentCube = Cubes[Cubes.Count - 1];
				Empties = new int[] {0, 1, 2, 3, 4, 5};
			}
			int FaceNumber = Empties[RNG.Range(0, Empties.Length)];
			MovementType Movement = (MovementType) UseMovements[RNG.Range(0, 6)];
			_CurrentCube.ShowOrder.Add(FaceNumber);
			_CurrentCube.Faces[FaceNumber].Present = true;
			_CurrentCube.Faces[FaceNumber].Number = RNumber;
			_CurrentCube.Faces[FaceNumber].ModuleID = GreenNumber;
			_CurrentCube.Faces[FaceNumber].TwitchID = TwitchID == -1 ? GreenNumber + 2 : TwitchID;
			_CurrentCube.Faces[FaceNumber].BackMovement = Movement;
			bool UseSecondary = Movement == MovementType.Clockwise || Movement == MovementType.CounterClockwise;
			_CurrentCube.Faces[FaceNumber].UseSecondary = UseSecondary;
			if(UseSecondary)
				_CurrentCube.Faces[FaceNumber].SecondaryBackMovement = (MovementType) UseMovements[RNG.Range(0, 4)];
			RNum.text = RNumber.ToString();
			FaceNum.text = (FaceNumber+1).ToString();
			Move.text = MovementCharacters[UseSecondary ? _CurrentCube.Faces[FaceNumber].SecondaryBackMovement : Movement];
			FacesAdded++;
			StartCoroutine(ChangeColor(RNum.color, c => RNum.color = c));
			StartCoroutine(ChangeColor(FaceNum.color, c => FaceNum.color = c));
			StartCoroutine(ChangeColor(Move.color, c => Move.color = c));
			if (UseSecondary)
			{
				yield return new SkipRequeue(RotateModule(4.3f, Movement));
				yield return new WaitForSeconds(.3f);
			}
			else yield return new WaitForSeconds(4.5f);
		}
		if (ReadyToSolve)
		{
			for (int i = 0; i < FacesPerStage; i++) yield return null;
			ReadyModules += 1;
			StartCoroutine(ShowKeypad());
		}
	}

	private IEnumerator ShowKeypad()
	{
		yield return new WaitUntil(() => ReadyModules>=MaxID);
		FinalNumber = (RawNumber + CalculateStage(StageSolves + 1) + GreenNumber);
		Logger("Ready to solve: "+FinalNumber);
		FlashingObjects.gameObject.SetActive(false);
		ShowButtons();
		DisplayText.transform.parent.gameObject.SetActive(true);
		ReadyToSolve = true;
	}

	private IEnumerator ChangeColor(Color start, Action<Color> NewColor)
	{
		var r = start.r;
		var g = start.g;
		var b = start.b;
		var a = start.a;
		for (int i = 0; i < 64; i++)
		{
			a += 3.984375f / 255f;
			var c = new Color(r, g, b, a);
			NewColor(c);
			yield return new WaitForSeconds(0.025f);
		}
		for (int i = 0; i < 64; i++)
		{
			a -= 3.984375f / 255f;
			var c = new Color(r, g, b, a);
			NewColor(c);
			yield return new WaitForSeconds(0.025f);
		}
	}
	
	IEnumerator RotateModule(float duration, MovementType movement)
    {
	    float startRotationy = 360;
	    float endRotation = 270;
	    int Multiplier = movement == MovementType.Clockwise ? 1 : -1;
		startRotationy = FlashingObjects.localEulerAngles.x;
	    endRotation = startRotationy + 360.0f;
	    float t = 0.0f;
	    while ( t  < duration )
	    { 
		    t += Time.deltaTime;
		    float yRotation = Mathf.Lerp(startRotationy, endRotation, t / duration) % 360.0f*Multiplier;
		    FlashingObjects.localEulerAngles = new Vector3(0, yRotation, 0);
		    yield return null;
	    }
	    FlashingObjects.localEulerAngles = new Vector3(0, 0, 0);
    }
    
	private string CalculateStage(int SolvedModules)
	{
		Logger(String.Format("Calculating stage for {0} modules", SolvedModules));
		string AllSum = "";
		foreach (Cube cube in Cubes)
		{
			Logger("New cube");
			int CurrentFace = 3;
			int TopFace = 5;
			int RightFace = 1;
			int LeftFace = 2;
			int BottomFace = 0;
			int OppositeFace = Opposites[CurrentFace];
			bool CurrentFacePresent = cube.Faces[CurrentFace].Present;
			bool OppositeFacePresent = cube.Faces[OppositeFace].Present;
			Action<MovementType, bool> HandleMove = null;
			HandleMove = (move, CurrentPresent) =>
			{
				Logger("Current face, front side: " + (CurrentFace+1)+" "+cube.Faces[CurrentFace].FrontMovement+" "+cube.Faces[CurrentFace].SecondaryFrontMovement);
				Logger("Opposite face, back side: " + (OppositeFace+1)+" "+cube.Faces[OppositeFace].BackMovement+" "+cube.Faces[OppositeFace].SecondaryBackMovement);
				Logger("Current face present: " + CurrentFacePresent);
				Logger("Opposite face present: " + OppositeFacePresent);
				Logger(String.Format("Top face: {0}, Bottom face: {1}, Right face: {2}, Left face: {3}", TopFace+1, BottomFace+1, RightFace+1, LeftFace+1));
				switch (move)
				{
					case MovementType.None: break;
					case MovementType.Up:
						TopFace = CurrentFace;
						CurrentFace = BottomFace;
						BottomFace = OppositeFace;
						break;
					case MovementType.Down:
						BottomFace = CurrentFace;
						CurrentFace = TopFace;
						TopFace = OppositeFace;
						break;
					case MovementType.Left:
						LeftFace = CurrentFace;
						CurrentFace = RightFace;
						RightFace = OppositeFace;
						break;
					case MovementType.Right:
						RightFace = CurrentFace;
						CurrentFace = LeftFace;
						LeftFace = OppositeFace;
						break;
					case MovementType.Clockwise:
						int OldRight = RightFace;
						RightFace = TopFace;
						TopFace = LeftFace;
						LeftFace = BottomFace;
						BottomFace = OldRight;
						HandleMove(CurrentPresent ? cube.Faces[CurrentFace].SecondaryFrontMovement : cube.Faces[OppositeFace].SecondaryBackMovement, CurrentPresent);
						break;
					case MovementType.CounterClockwise:
						int OldLeft = LeftFace;
						LeftFace = TopFace;
						TopFace = RightFace;
						RightFace = BottomFace;
						BottomFace = OldLeft;
						HandleMove(CurrentPresent ? cube.Faces[CurrentFace].SecondaryFrontMovement : cube.Faces[OppositeFace].SecondaryBackMovement, CurrentPresent);
						break;
				}
			};
			Logger(String.Format("Number of moves: {0}", Mathf.CeilToInt(MovementTimesRaw * (SolvedModules % 20))));
			for (int i = 0; i < Mathf.CeilToInt(MovementTimesRaw * (SolvedModules%20)); i++)
			{
				OppositeFace = Opposites[CurrentFace];
				CurrentFacePresent = cube.Faces[CurrentFace].Present;
				OppositeFacePresent = cube.Faces[OppositeFace].Present;
				HandleMove(!CurrentFacePresent && !OppositeFacePresent ? MovementType.Down :
					CurrentFacePresent ? cube.Faces[CurrentFace].FrontMovement :
					cube.Faces[OppositeFace].BackMovement, CurrentFacePresent);
			}
			OppositeFace = Opposites[CurrentFace];
			CurrentFacePresent = cube.Faces[CurrentFace].Present;
			OppositeFacePresent = cube.Faces[OppositeFace].Present;
			Logger("New face: "+(CurrentFace+1));
			Logger("Front number: " + cube.Faces[CurrentFace].FrontNumber);
			Logger("Back number: " + cube.Faces[OppositeFace].BackNumber);
			Logger(String.Format("Front face present: {0}, Back face present: {1}", CurrentFacePresent, OppositeFacePresent));
			Logger("Adding: "+(!CurrentFacePresent && !OppositeFacePresent ? 30 :
				CurrentFacePresent ? cube.Faces[CurrentFace].FrontNumber :
				cube.Faces[OppositeFace].BackNumber));
			int n = !CurrentFacePresent && !OppositeFacePresent ? 30 :
				CurrentFacePresent ? cube.Faces[CurrentFace].FrontNumber :
				cube.Faces[OppositeFace].BackNumber;
			AllSum += n.ToString();
			CubeIndexes.Add(RawNumber.Length + AllSum.Length, ++CurrentCubeCalc);
			AllCubes.Add(cube);
		}
		return AllSum;
	}

	private void Logger(string message)
	{
		Debug.LogFormat("[Cube Synchronization #{0}] {1}", GreenNumber, message);
	}
}
