using System;
using System.Collections;
using System.Linq;
using System.Text;
using kOS;
using kOS.Safe;
using UnityEngine;
using kOS.Suffixed;

using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using EVAMove;

using System.Collections.Generic;
using System.Reflection;
using kOS.Utilities;

namespace kOS.AddOns.kOSEVA
{
	[kOSAddon("EVA")]
	[kOS.Safe.Utilities.KOSNomenclature("EVAAddon")]
	public class Addon : Suffixed.Addon
	{
		public Addon(SharedObjects shared) : base(shared)
		{
			InitializeSuffixes();
		}

		private void InitializeSuffixes()
		{

			AddSuffix("TOGGLE_RCS", new OneArgsSuffix<BooleanValue>(ToggleRCS, "Switch the RCS of the Pack <on|off>"));
			AddSuffix("RCS", new SetSuffix<BooleanValue>(GetRCS, SetRCS, "Query or set the status of the pack RCS"));
			AddSuffix("LIGHTS", new SetSuffix<BooleanValue>(GetLights, SetLights, "Query or set the status of the headlamps"));
			AddSuffix("VISOR", new SetSuffix<BooleanValue>(GetVisor, SetVisor));
			AddSuffix("HELMET", new SetSuffix<BooleanValue>(GetHelmet, SetHelmet));
			AddSuffix("DOEVENT", new TwoArgsSuffix<Suffixed.Part.PartValue, StringValue>(DoEvent, "Performs a Event on a others vessel part."));
			AddSuffix("LADDER_RELEASE", new NoArgsVoidSuffix(LadderRelease, "Release a grabbed ladder"));
			AddSuffix("LADDER_GRAB", new NoArgsVoidSuffix(LadderGrab, "Grab a nearby ladder"));
			AddSuffix("TURN_LEFT", new OneArgsSuffix<ScalarValue>(TurnLeft, "make the kerbal turn by <deg>"));
			AddSuffix("TURN_RIGHT", new OneArgsSuffix<ScalarValue>(TurnRight, "make the kerbal turn by <deg>"));
			AddSuffix("TURN_TO", new OneArgsSuffix<Vector>(TurnTo, "make the kerbal turn to <vector>"));
			AddSuffix("MOVE", new OneArgsSuffix<StringValue>(MoveKerbal, "make the kerbal move"));
			AddSuffix("BOARDPART", new OneArgsSuffix<Suffixed.Part.PartValue>(BoardPart, "Enters the Part"));
			AddSuffix("BOARD", new NoArgsVoidSuffix(DoBoard, "Boad a Nearby Vessel or Part"));
			AddSuffix("PLANTFLAG", new TwoArgsSuffix<StringValue, StringValue>(DoPlantFlag, "Plants a Flag"));
			AddSuffix("RUNACTION", new OneArgsSuffix<StringValue>(DoRunEvent, "Runs a Event by its name"));
			AddSuffix("ACTIONLIST", new NoArgsSuffix<ListValue>(ListEvents, "List of all event names"));
			AddSuffix("ANIMATIONLIST", new NoArgsSuffix<ListValue>(ListAnimations, "List of all animation names"));
			AddSuffix("PLAYANIMATION", new OneArgsSuffix<StringValue>(PlayAnimation, "Runs a build-in animation by its internal name"));
			AddSuffix("LOADANIMATION", new OneArgsSuffix<StringValue>(LoadAnimation, "Runs a custom animation by its relative pathname"));
			AddSuffix("STOPANIMATION", new OneArgsSuffix<StringValue>(StopAnimation, "Stops the Animation"));
			AddSuffix("STOPALLANIMATIONS", new NoArgsVoidSuffix(StopAllAnimations, "Stops all Animations"));
			AddSuffix(new[] { "GOEVA", "EVA" }, new OneArgsSuffix<CrewMember>(GoEVA, "Compliments a Kerbal to the Outside"));
			AddSuffix("DUMPEXPERIMENTS", new NoArgsVoidSuffix(DumpExperiments));
			AddSuffix("NEUTRALIZE", new SetSuffix<BooleanValue>(() => evacontrol.Neutralize, value => evacontrol.Neutralize = value));
			AddSuffix("STARBOARD", new SetSuffix<ScalarValue>(() => evacontrol.MovementThrottle.x, value => { var throttle = evacontrol.MovementThrottle; throttle.x = Mathf.Clamp(value, -1, 1); evacontrol.MovementThrottle = throttle; }));
			AddSuffix("TOP", new SetSuffix<ScalarValue>(() => evacontrol.MovementThrottle.y, value => { var throttle = evacontrol.MovementThrottle; throttle.y = Mathf.Clamp(value, -1, 1); evacontrol.MovementThrottle = throttle; }));
			AddSuffix("FORE", new SetSuffix<ScalarValue>(() => evacontrol.MovementThrottle.z, value => { var throttle = evacontrol.MovementThrottle; throttle.z = Mathf.Clamp(value, -1, 1); evacontrol.MovementThrottle = throttle; }));
			AddSuffix("MOVETHROTTLE", new SetSuffix<Vector>(() => new Vector(evacontrol.MovementThrottle), value => evacontrol.MovementThrottle = value.ToVector3()));
			AddSuffix("JUMP", new NoArgsVoidSuffix(Jump));
			AddSuffix("SPRINT", new SetSuffix<BooleanValue>(() => evacontrol.Sprint, value => evacontrol.Sprint = value));
			AddSuffix("STATE", new Suffix<StringValue>(() => kerbaleva.fsm.currentState.name));
   			AddSuffix("TRANSFERCREW", new TwoArgsSuffix<BooleanValue, CrewMember, Suffixed.Part.PartValue>(TransferCrew, "Transfer CrewMember to the Part"));

			// Set a default bootfilename, when no other has been set.
			if (shared.Vessel.isEVA && shared.KSPPart.GetComponentCached<Module.kOSProcessor>(ref _myprocessor).bootFile.ToLower() == "none" )
			{
				Module.kOSProcessor  myproc = null;
				shared.KSPPart.GetComponentCached<Module.kOSProcessor>(ref myproc);
				myproc.bootFile = "/boot/eva";
			}

			CheckEvaController();

#if DEBUG
			AddSuffix("LS", new NoArgsSuffix<ListValue>(listfields, ""));
			AddSuffix("LSF", new NoArgsSuffix<ListValue>(listfunctions, ""));
#endif

		}

		private void Jump()
		{
			CheckEvaController();
			evacontrol.Jump = true;
		}

		private BooleanValue GetHelmet()
		{
			CheckEvaController();
			return kerbaleva.isHelmetEnabled;
		}

		private void SetHelmet(BooleanValue value)
		{
			CheckEvaController();
			kerbaleva.ToggleHelmet(value);
		}

		private BooleanValue GetVisor()
		{
			CheckEvaController();
			return kerbaleva.visorState == KerbalEVA.VisorStates.Lowered;
		}

		private void SetVisor(BooleanValue value)
		{
			CheckEvaController();
			if (value)
			{
				kerbaleva.LowerVisor();
			}
			else
			{
				kerbaleva.RaiseVisor();
			}
		}

		private void DumpExperiments()
		{
			if (!shared.Vessel.isEVA) return;

			shared.Vessel.rootPart.GetComponentCached<ModuleScienceContainer>(ref scienceContainer);

			foreach (var data in scienceContainer.GetData())
			{
				scienceContainer.DumpData(data);
			}

			foreach (var experiment in shared.Vessel.rootPart.Modules.GetModules<ModuleScienceExperiment>())
			{
				if (experiment.resettable)
				{
					experiment.ResetExperiment();
				}
			}
		}

		internal Module.kOSProcessor _myprocessor = null;
		internal ModuleScienceContainer scienceContainer;
		public KerbalEVA kerbaleva = null;
		internal EvaController evacontrol = null;
		internal bool rcs_state = false;

		public override BooleanValue Available()
		{
			return true;
		}



		#region Suffix functions
#if DEBUG 
		private ListValue listfields ()
		{
			ListValue vectors = new ListValue();
			List<FieldInfo> fields = new List<FieldInfo>(typeof(KerbalEVA).GetFields(
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
			var vectorFields = new List<FieldInfo>(fields.Where<FieldInfo>(f => f.FieldType.Equals(typeof(Vector3))));
			foreach (var vector in vectorFields)
			{
				vectors.Add(new StringValue(vector.Name + "	\t   " + vector.FieldType.ToString() ));

			}
			foreach (var vector in fields)
			{
				vectors.Add(new StringValue(vector.Name + "	\t   " + vector.FieldType.ToString()));

			}

			return vectors;
		}

		private ListValue listfunctions()
		{
			ListValue vectors = new ListValue();
			List<MethodInfo> methods = new List<MethodInfo>(typeof(KerbalEVA).GetMethods(
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
			//var vectorFields = new List<FieldInfo>(fields.Where<FieldInfo>(f => f.FieldType.Equals(typeof(Vector3))));
			foreach (var method in methods)
			{
				var parameters = method.GetParameters().ToArray();
				string paraname = "( ";
				foreach ( var para in parameters )
				{
					paraname = paraname + para.ParameterType.ToString() + " ";
					paraname = paraname + para.Name.ToString() + " , ";
				}
				paraname = paraname + " )";
				vectors.Add(new StringValue(method.Name + "	\t	" + paraname));

			}
			return vectors;
		}

#endif
		private void ToggleRCS(BooleanValue state)
		{
			CheckEvaController();
			if (state.Value != rcs_state)
			{
				try
				{
					KerbalEVAUtility.RunEvent(kerbaleva, "Pack Toggle");
					rcs_state = state;
				}
				catch { }
			}
		}

		private BooleanValue GetRCS()
		{
			CheckEvaController();
			try
			{
				return kerbaleva.JetpackDeployed;
			}
			catch { return false; }
		}

		private void SetRCS(BooleanValue state)
		{
			CheckEvaController();
			try
			{
				if (kerbaleva.JetpackDeployed != state)
				{
					kerbaleva.ToggleJetpack();
				}
			}
			catch { }
		}
		private BooleanValue GetLights()
		{
			CheckEvaController();
			try
			{
				return kerbaleva.lampOn;
			}
			catch { return false; }
		}

		private void SetLights(BooleanValue state)
		{
			CheckEvaController();
			try
			{
				if (kerbaleva.lampOn != state)
				{
					kerbaleva.ToggleLamp();
				}
			}
			catch { }
		}

		private void DoEvent(Suffixed.Part.PartValue part , StringValue eventname)
		{
			CheckEvaController();
			var mypart = part.Part;
			PartModule mypartmodule = null;
			Debug.LogWarning("kOS-EVA: [DOEVENT] part: "+ mypart.name + "dst: " + Math.Round((mypart.transform.position - kerbaleva.vessel.rootPart.transform.position).magnitude,2));
			if   (Vector3d.Magnitude(mypart.transform.position - kerbaleva.vessel.rootPart.transform.position) < 2.5)
			{
				Debug.LogWarning("kOS-EVA: [DOEVENT] distance ok:" );
				PartModule[] allpartmodules = mypart.GetComponents<PartModule>();
				foreach (var pm in allpartmodules)
				{
					if ( pm.Events.Where(x => x.GUIName.ToLower().StartsWith(eventname.ToLower()) ).FirstOrDefault() == null )
					{
						continue;
					}
					Debug.Log("kOS-EVA: [DOEVENT] Partmodule found:" + pm.moduleName);
					mypartmodule = pm;

				}

				if (mypartmodule == null)
				{
					Debug.LogWarning("kOS-EVA: [DOEVENT] Partmodule not found ");
					return;
				}
				BaseEvent my_event = mypartmodule.Events.Where(x => x.GUIName.ToLower().StartsWith(eventname.ToLower()) ).FirstOrDefault();
				if (my_event == null)
				{
					Debug.LogWarning("kOS-EVA: [DOEVENT] Event not found ");
					return;
				}
				else
				{
					Debug.Log("kOS-EVA: [DOEVENT] Invoking:" + my_event.GUIName);
					my_event.Invoke();
				}
			} else
			{
				Debug.LogWarning("kOS-EVA: [DOEVENT] Part Out of Range: " + Math.Round(Vector3d.Magnitude(mypart.transform.position - shared.Vessel.rootPart.transform.position),2) + " > 2.5");
			}
		}


		private void BoardPart(Suffixed.Part.PartValue toboard)
		{
			CheckEvaController();
			kerbaleva.BoardPart(toboard.Part);

		}

		private void DoBoard()
		{
			CheckEvaController();
			try
			{
					KerbalEVAUtility.RunEvent(kerbaleva, "Boarding Part");
			}
			catch { }

		}

		private void LadderGrab()
		{
			CheckEvaController();
			try
			{
				KerbalEVAUtility.RunEvent(kerbaleva, "Ladder Grab Start");
			}
			catch { }

		}
		private void LadderRelease()
		{
			CheckEvaController();
			try { 
				KerbalEVAUtility.RunEvent(kerbaleva, "Ladder Let Go");
			}
			catch { }

		}


		private void DoRunEvent(StringValue eventname)
		{
			CheckEvaController();
			try
			{
				KerbalEVAUtility.RunEvent(kerbaleva, eventname.ToString());
			}
			catch { }

		}

		private ListValue ListEvents()
		{
			CheckEvaController();
			ListValue events = new ListValue();
			foreach (var evaevent in KerbalEVAUtility.GetEVAEvents(kerbaleva, KerbalEVAUtility.GetEVAStates(kerbaleva) ))
			{
				events.Add(new StringValue(evaevent.name));
			}
			return events;

		}



		// Code from Flightcontroller
		private void GoEVA (CrewMember kerbal)
		{
			foreach (var crewMember in shared.Vessel.GetVesselCrew())
			{
				if (crewMember.name.ToLower() == kerbal.Name.ToLower())
				{
					FlightEVA.fetch.StartCoroutine(GoEVADelayed(crewMember.KerbalRef));
					return;
				}
			}

		}

		private IEnumerator GoEVADelayed(Kerbal kerbal)
		{
			yield return new WaitForFixedUpdate();
			FlightEVA.SpawnEVA(kerbal);
		}

		public void DoPlantFlag(StringValue flagname, StringValue flagtext)
		{
			CheckEvaController();
			if (!shared.Vessel.isEVA || !kerbaleva.part.GroundContact)
			{
				return;
			}
			PlayAnimation(new StringValue("idle"));
			StopAnimation(new StringValue("idle"));
			PlayAnimation(new StringValue("flag_plant"));
			var flag = FlagSite.CreateFlag( (shared.Vessel.GetWorldPos3D() + shared.Vessel.transform.forward * 0.26f - shared.Vessel.transform.up * 0.20f) , shared.Vessel.transform.rotation, kerbaleva.part );
			flag.placedBy = kerbaleva.vessel.vesselName;
			flag.PlaqueText = flagtext.ToString();
			flag.vessel.vesselName = flagname;

			kerbaleva.part.protoModuleCrew[0].flightLog.AddEntryUnique(FlightLog.EntryType.PlantFlag, kerbaleva.vessel.orbit.referenceBody.name);
			kerbaleva.part.protoModuleCrew[0].UpdateExperience();
			int count = FlightGlobals.VesselsLoaded.Count;
			while (count-- > 0)
			{
				Vessel vessel = FlightGlobals.VesselsLoaded[count];
				if (vessel == null)
				{
					continue;
				}
				if (!vessel.loaded)
				{
					continue;
				}
				if (vessel == FlightGlobals.ActiveVessel)
				{
					continue;
				}
				if (vessel.vesselType == VesselType.EVA)
				{
					ProtoCrewMember protoCrewMember = vessel.GetVesselCrew()[0];
					protoCrewMember.flightLog.AddEntryUnique(FlightLog.EntryType.PlantFlag, kerbaleva.vessel.orbit.referenceBody.name);
					protoCrewMember.UpdateExperience();
					continue;
				}
				if (vessel.situation != Vessel.Situations.LANDED)
				{
					if (vessel.situation != Vessel.Situations.SPLASHED)
					{
						if (vessel.situation != Vessel.Situations.PRELAUNCH)
						{
							continue;
						}
					}
				}
				List<ProtoCrewMember> vesselCrew = vessel.GetVesselCrew();
				int count2 = vesselCrew.Count;
				while (count2-- > 0)
				{
					ProtoCrewMember protoCrewMember2 = vesselCrew[count2];
					protoCrewMember2.flightLog.AddEntryUnique(FlightLog.EntryType.PlantFlag, kerbaleva.vessel.orbit.referenceBody.name);
					protoCrewMember2.UpdateExperience();
				}
			}
		}

		/*
		public void DoPlantFlagOld()
		{
			if (!shared.Vessel.isEVA)
			{
				return;
			}
			if (kerbaleva.part.GroundContact)
			{
				try
				{
					KerbalEVAUtility.RunEvent(kerbaleva, "Flag Plant Started");
				}
				catch { }
			}
		}
		*/

		private ListValue ListAnimations ()
		{
			CheckEvaController();
			ListValue animations = new ListValue();
			foreach (AnimationState state in kerbaleva.GetComponent<Animation>() )
			{
				animations.Add(new StringValue(state.name));
			}
			return animations;
		}

		private void PlayAnimation(StringValue name)
		{
			Animation _kerbalanimation = null;
			shared.Vessel.GetComponentCached<Animation>(ref _kerbalanimation);
			_kerbalanimation.CrossFade(name.ToString());
		}

		private void StopAnimation(StringValue name)
		{
			Animation _kerbalanimation = null;
			shared.Vessel.GetComponentCached<Animation>(ref _kerbalanimation);
			_kerbalanimation.Stop(name);
			_kerbalanimation.CrossFade("idle",0.3f,PlayMode.StopSameLayer);
		}

		private void StopAllAnimations()
		{
			Animation _kerbalanimation = null;
			shared.Vessel.GetComponentCached<Animation>(ref _kerbalanimation);
		  //  _kerbalanimation.Stop();
			_kerbalanimation.CrossFade("idle", 0.3f, PlayMode.StopAll);
		}

		private void LoadAnimation (StringValue path)
		{
			Animation _kerbalanimation = null;
			shared.Vessel.GetComponentCached<Animation>(ref _kerbalanimation);
			var kerbaltransform = shared.Vessel.transform;
			KerbalAnimationClip myclip = new KerbalAnimationClip();
			myclip.LoadFromURL(path.ToString());
			myclip.Initialize(_kerbalanimation, kerbaltransform);
		}

		private void CheckEvaController()
		{
			if (shared.Vessel.isEVA == false ) { return; } 
			if (evacontrol == null)
			{
				Debug.LogWarning("kOSEVA: Start init EvaController");
				this.kerbaleva = shared.Vessel.GetComponentCached<KerbalEVA>(ref kerbaleva);
				evacontrol = shared.Vessel.GetComponentCached<EvaController>(ref evacontrol);

				Debug.LogWarning("kOSEVA: Stop init EvaController");
			}

		}

		private void MoveKerbal(StringValue direction)
		{
			if (!shared.Vessel.isEVA) { return; }
			CheckEvaController();

			Command command = (Command)Enum.Parse(typeof(Command), direction, true);
			Debug.Log("EVA Command: " + command.ToString());
			
			switch (command)
			{
				case Command.Forward:
					evacontrol.MovementThrottle = new Vector3(0, 0, 1);
					break;
				case Command.Backward:
					evacontrol.MovementThrottle = new Vector3(0, 0, -1);
					break;
				case Command.Left:
					evacontrol.MovementThrottle = new Vector3(-1, 0, 0);
					break;
				case Command.Right:
					evacontrol.MovementThrottle = new Vector3(1, 0, 0);
					break;
				case Command.Up:
					evacontrol.MovementThrottle = new Vector3(0, 1, 0);
					break;
				case Command.Down:
					evacontrol.MovementThrottle = new Vector3(0, -1, 0);
					break;
				case Command.LookAt:
				case Command.Stop:
					evacontrol.MovementThrottle = Vector3.zero;
					break;
			}
		}

		private void TurnLeft(ScalarValue degrees)
		{
			if (!shared.Vessel.isEVA) { return; }
			CheckEvaController();
			this.evacontrol.LookDirection = v_rotate(kerbaleva.vessel.transform.forward, kerbaleva.vessel.transform.right, -degrees.GetDoubleValue());
		}
		private void TurnRight(ScalarValue degrees)
		{
			if (!shared.Vessel.isEVA) { return; }
			CheckEvaController();
			this.evacontrol.LookDirection = v_rotate(kerbaleva.vessel.transform.forward, kerbaleva.vessel.transform.right, degrees.GetDoubleValue());
		}

		private void TurnTo(Vector direction)
		{
			if (!shared.Vessel.isEVA) { return; }
			CheckEvaController();
			this.evacontrol.LookDirection = direction.ToVector3D();
		}
  		
    		private BooleanValue TransferCrew(CrewMember kerbal, Suffixed.Part.PartValue aimedPart)
		{
    			Part destPart = null;
		    	ProtoCrewMember safeCrewMember = null;

    			foreach (var vesselPart in shared.Vessel.Parts)
    			{
        			if (vesselPart.uid() == aimedPart.Part.uid())
        			{
            				destPart = vesselPart;
            				break;
        			}
    			}
    			
       			foreach (var vesselCrew in shared.Vessel.GetVesselCrew())
    			{
        			if (vesselCrew.name.ToLower() == kerbal.Name.ToLower())
        			{
            				safeCrewMember = vesselCrew;
            				break;
        			}
    			}

    			if (safeCrewMember == null || destPart == null || destPart.protoModuleCrew.Count() >= destPart.CrewCapacity)
    			{
        			return false;
    			}

    			var transfer = CrewTransfer.Create(safeCrewMember.seat.part, safeCrewMember, delegate { });
    			try
    			{
        			transfer.MoveCrewTo(destPart);
    			}
    			catch (Exception ex)
    			{
        			Debug.LogWarning("kOSEVA: CrewTransfer failed: " + ex.ToString());
    			}
    			return true;
		}
		#endregion

		#region internal functions
		internal Vector3d v_rotate(Vector3d vec_from, Vector3d vec_to, double deg)
		{
			double deginrad = Mathf.Deg2Rad * deg;
			return ((Math.Cos(deginrad) * vec_from) + (Math.Sin(deginrad) * vec_to));
		}
		#endregion

	}
}
