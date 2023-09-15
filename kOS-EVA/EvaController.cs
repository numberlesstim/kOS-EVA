using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using kOS.Suffixed;

using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using HarmonyLib;

namespace EVAMove
{
	public enum Command
	{
		Forward,
		Backward,
		Left,
		Right,
		Up,
		Down,
		LookAt,
		Stop
	}

	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	public class EvaControllerPatch : MonoBehaviour
	{
		void Awake()
		{
			var harmony = new Harmony("kOSEVA");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}

		[HarmonyPatch(typeof(KerbalEVA), "HandleMovementInput")]
		class KerbalEVA_HandleMovementInput_Patch
		{
			static void Prefix(KerbalEVA __instance)
			{
				var evaController = __instance.part.FindModuleImplementing<EvaController>();

				if (evaController != null)
				{
					evaController.HandleMovementInput_Prefix();
				}
			}

			static void Postfix(KerbalEVA __instance)
			{
				var evaController = __instance.part.FindModuleImplementing<EvaController>();

				if (evaController != null)
				{
					evaController.HandleMovementInput_Postfix();
				}
			}
		}
	}

	public class EvaController : PartModule
	{
		public Vector3 MovementThrottle
		{
			get { return m_movementThrottle; }
			set
			{
				if (!m_kosControl)
				{
					m_lookDirection = m_kerbalEVA.transform.forward;
					m_kosControl = true;
				}

				m_movementThrottle = value;
			}
		} 

		public Vector3 LookDirection
		{
			get { return m_lookDirection; }
			set
			{
				if (!m_kosControl)
				{
					m_movementThrottle = Vector3.zero;
					m_kosControl = true;
				}

				m_lookDirection = value;
			}
		}

		public bool Neutralize
		{
			get { return !m_kosControl; }
			set { m_kosControl = !value; }
		}

		Vector3 m_movementThrottle;
		Vector3 m_lookDirection;

		KerbalEVA m_kerbalEVA;
		bool m_kosControl;

		public override void OnAwake()
		{
			base.OnAwake();
			m_kerbalEVA = part.FindModuleImplementing<KerbalEVA>();
		}

		internal void HandleMovementInput_Prefix()
		{
			m_kerbalEVA.CharacterFrameModeToggle = m_kosControl;

			if (!m_kosControl) return;

			Vector3 tgtRpos =
				MovementThrottle.z * m_kerbalEVA.transform.forward +
				MovementThrottle.x * m_kerbalEVA.transform.right;

			Vector3 packTgtRpos = tgtRpos + MovementThrottle.y * m_kerbalEVA.transform.up;

			m_kerbalEVA.tgtRpos = tgtRpos;
			m_kerbalEVA.packTgtRPos = packTgtRpos;
			m_kerbalEVA.ladderTgtRPos = packTgtRpos; // for now, same as jetpack (so up/down match)
		}

		internal void HandleMovementInput_Postfix()
		{
			if (!m_kosControl) return;

			// rotation needs to be done after the main method or else it will get overwritten
			m_kerbalEVA.tgtFwd = m_lookDirection;

			if (m_kerbalEVA.tgtRpos == Vector3.zero)
			{
				m_kerbalEVA.tgtRpos = m_lookDirection * 0.0001f;
			}

			// parachuteInput gets cleared in handleMovementInput, so we need to set it in postfix
		}
	}

}

#if false

namespace EVAMove
{

	public class EvaController : PartModule
	{

		public static EvaController instance = null;

 //	   public EvaController() { if (FlightGlobals.ActiveVessel.isEVA) {  Debug.LogWarning("EvaController Created"); instance = this; } }

		public Command order = Command.Stop;
		public KerbalEVA eva = null;
		public Vector3d lookdirection = Vector3d.zero;
		public float rotationdeg;
		internal string currentanimation = null;
		internal string tgtanimation = null;
		public FieldInfo eva_tgtFwd = null;
		public FieldInfo eva_tgtUp = null;
		public FieldInfo eva_tgtRpos = null;
		public FieldInfo eva_packTgtRPos = null;
		public FieldInfo eva_packLinear = null;
		internal bool once = true;
		internal float lastkeypressed = 0.0f;
		internal bool initialized = false;

		#region public function

		//	OnStart (StartState state)
   /*	 public override void OnStart(StartState state)
		{
			// check for KerbalBot and trait here and remove kOS Module not allowed.
			if (ResearchAndDevelopment.GetTechnologyState("miniaturization") == RDTech.State.Unavailable)
			{
				Debug.LogWarning("EvaController Initialize(): " + vessel.name + " removing modules");
				this.part.RemoveModule(this.part.GetComponent<kOS.Module.kOSProcessor>());
			//	Destroy(this);
				//this.part.RemoveModule(this.part.GetComponent<EvaController>());
			}
		}
		*/

		public void Initialize()
		{
			Debug.LogWarning("EvaController Initialize: " + vessel.vesselName);
			eva = vessel.GetComponent<KerbalEVA>();
			if (eva == null || !eva.vessel.isEVA)
			{
				Debug.LogWarning("EvaController destroyed on Initialize(): " + vessel.vesselName + ": not EVA");
				Destroy(this);
			}



			eva_tgtRpos = typeof(KerbalEVA).GetField("tgtRpos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			eva_packTgtRPos = typeof(KerbalEVA).GetField("packTgtRPos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			eva_tgtFwd = typeof(KerbalEVA).GetField("tgtFwd", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			eva_tgtUp = typeof(KerbalEVA).GetField("tgtUp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			eva_packLinear = typeof(KerbalEVA).GetField("packLinear", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			initialized = true;
		}



		public void FixedUpdate()
		{
			if (initialized == false) { Initialize(); }


			if (eva == null || !eva.vessel.isEVA )
			{
				   Debug.LogWarning("EvaController destroyed on FixexdUpdate: " + vessel.vesselName + ": not EVA" );
				   Destroy(this);

			}
			// priority: 0. Ragdoll recover 1. onladder 2. in water 3. on land 4. flying around
			try
			{
				if (eva.fsm.CurrentState.name == "Ragdoll")
				{
					TryRecoverFromRagdoll();
					return;
				}

				if (eva.fsm.CurrentState.name == "Recover")
				{
					return;
				}
			} catch { }

			if (eva.OnALadder)
			{
				DoMoveOnLadder();
				return;
			}

			if (eva.part.WaterContact)
			{
				DoMoveInWater();
				return;
			}

			if (eva.vessel.situation == Vessel.Situations.LANDED || eva.vessel.situation == Vessel.Situations.SPLASHED)
			{
				DoMoveOnLand();
				return;
			}

			if (eva.JetpackDeployed)
			{
				DoMoveInSpace();
				return;
			}

		}


		void Update()
		{
			if (initialized == false) { Initialize(); }
			Animation _kerbalanimation = null;
			eva.vessel.GetComponentCached<Animation>(ref _kerbalanimation);
			if (!_kerbalanimation.IsPlaying(tgtanimation))
			{
				StopAllAnimations();
				PlayAnimation(tgtanimation);
			}
			CheckKeys();
		}


		void OnDestroy()
		{
			instance = null;
		}

		#endregion

		#region internal functions



		internal void CheckKeys()
		{
			if (!vessel.isEVA || vessel.id != FlightGlobals.ActiveVessel.id ) { return;  }


			if (Input.GetKeyDown(KeyCode.Alpha1)) {
				vessel.ActionGroups.ToggleGroup(KSPActionGroup.Custom01);
			}
			if (Input.GetKeyDown(KeyCode.Alpha2))
			{
				vessel.ActionGroups.ToggleGroup(KSPActionGroup.Custom02);
			}
			if (Input.GetKeyDown(KeyCode.Alpha3))
			{
				vessel.ActionGroups.ToggleGroup(KSPActionGroup.Custom03);
			}
			if (Input.GetKeyDown(KeyCode.Alpha4))
			{
				vessel.ActionGroups.ToggleGroup(KSPActionGroup.Custom04);
			}
			if (Input.GetKeyDown(KeyCode.Alpha5))
			{
				vessel.ActionGroups.ToggleGroup(KSPActionGroup.Custom05);
			}
			if (Input.GetKeyDown(KeyCode.Alpha6))
			{
				vessel.ActionGroups.ToggleGroup(KSPActionGroup.Custom06);
			}
			if (Input.GetKeyDown(KeyCode.Alpha7))
			{
				vessel.ActionGroups.ToggleGroup(KSPActionGroup.Custom07);
			}
			if (Input.GetKeyDown(KeyCode.Alpha8))
			{
				vessel.ActionGroups.ToggleGroup(KSPActionGroup.Custom08);
			}
			if (Input.GetKeyDown(KeyCode.Alpha9))
			{
				vessel.ActionGroups.ToggleGroup(KSPActionGroup.Custom09);
			}
			if (Input.GetKeyDown(KeyCode.Alpha0))
			{
				vessel.ActionGroups.ToggleGroup(KSPActionGroup.Custom10);
			}
		}


		internal void TryRecoverFromRagdoll()
		{
			Debug.Log("KOSEVA: Trying to recover kerbal.");
			if (eva.canRecover && eva.fsm.TimeAtCurrentState > 1.21f && eva.part.GroundContact)
			{
				KerbalEVAUtility.RunEvent(eva, "Recover Start");
			}
		}

		// only up and down allowed
		internal void DoMoveOnLadder()
		{
			float dtime = Time.deltaTime;
			switch (order)
			{ 
				case Command.Up:
					eva.vessel.transform.position += eva.ladderClimbSpeed * dtime * eva.ladderPivot.up;
					tgtanimation = "ladder_up";
					break;
				case Command.Down:
					eva.vessel.transform.position -= eva.ladderClimbSpeed * dtime * eva.ladderPivot.up;
					tgtanimation = "ladder_down";
					break;
				case Command.Stop:
					tgtanimation = "ladder_idle";
					break;
				default:
					tgtanimation = "ladder_idle";
					break;
			}

		}
		// we only allow turning and forward in water
		internal void DoMoveInWater()
		{
			float dtime = Time.deltaTime;
			switch (order)
			{
				case Command.Forward:
					eva.vessel.transform.position += eva.swimSpeed * dtime * eva.vessel.transform.forward;
					tgtanimation = "swim_forward";
					break;
				case Command.LookAt:
					tgtanimation = "swim_idle";
					if (Vector3d.Angle(eva.vessel.transform.forward, Vector3d.Exclude(eva.vessel.transform.up, lookdirection)) < 0.2)
					{
						order = Command.Stop;
						break;
					}
					var step = eva.turnRate;
					Quaternion from = eva.vessel.transform.rotation;
					Quaternion to = Quaternion.LookRotation(lookdirection, eva.vessel.transform.up);
					Quaternion result = Quaternion.RotateTowards(from, to, step);
					eva.vessel.SetRotation(result);
					break;
				case Command.Stop:
					tgtanimation = "swim_idle";
					break;
				default:
					tgtanimation = "swim_idle";
					break;
			}

		}



		internal void DoMoveOnLand()
		{
			if (eva.CharacterFrameMode && once) { Debug.LogWarning("Framemode active"); once = false; }
			float dtime = Time.deltaTime;
			switch (order)
			{
				case Command.Forward:
					eva.vessel.transform.position += eva.walkSpeed * dtime * eva.vessel.transform.forward;
					tgtanimation = eva.vessel.geeForce > eva.minWalkingGee ? "wkC_forward" : "wkC_loG_forward";
					break;
				case Command.Backward:
					eva.vessel.transform.position -= eva.strafeSpeed * dtime * eva.vessel.transform.forward;
					// couldn't find a low-g backward animation
					tgtanimation = eva.vessel.geeForce > eva.minWalkingGee ? "wkC_backwards" : "wkC_backwards";
					break;
				case Command.Left:
					eva.vessel.transform.position -= eva.strafeSpeed * dtime * eva.vessel.transform.right;
					tgtanimation = eva.vessel.geeForce > eva.minWalkingGee ? "wkC_sideLeft" : "wkC_loG_sideLeft";
					break;
				case Command.Right:
					eva.vessel.transform.position += eva.strafeSpeed * dtime * eva.vessel.transform.right;
					tgtanimation = eva.vessel.geeForce > eva.minWalkingGee ? "wkC_sideRight" : "wkC_loG_sideRight";
					break;
				case Command.Up:
					break;
				case Command.Down:
					break;
				case Command.LookAt:
					if (Vector3d.Angle(eva.vessel.transform.forward, Vector3d.Exclude(eva.vessel.transform.up, lookdirection)) < 0.2)
					{
						order = Command.Stop;
						tgtanimation = "idle";
						break;
					}
					//var step = eva.turnRate * dtime;
					var step = eva.turnRate;
					Quaternion from = eva.vessel.transform.rotation;
					Quaternion to = Quaternion.LookRotation(lookdirection, eva.vessel.transform.up);
					Quaternion result = Quaternion.RotateTowards(from, to, step);
					eva.vessel.SetRotation(result);
					tgtanimation = Vector3d.Angle(eva.vessel.transform.right, Vector3d.Exclude(eva.vessel.transform.up, lookdirection)) < 90 ? "leftTurn" : "rightTurn";
					break;
				case Command.Stop:
					tgtanimation = "idle";
					break;
				default:
					break;
			}
		}

		internal void DoMoveInSpace()
		{
			float dtime = Time.deltaTime;
			if  (once) { Debug.LogWarning("linPower: " + eva.linPower.ToString() + "   rotation Power:  " + eva.rotPower.ToString()  ); once = false; }
			switch (order)
			{
				case Command.Forward:
					//this.eva_packTgtRPos.SetValue(eva, eva.transform.forward);
					//this.eva_Vtgt.SetValue(eva, eva.transform.forward );
					eva.part.Rigidbody.AddForce(eva.transform.forward * dtime * 2f, ForceMode.Force);
					//this.eva_packLinear.SetValue(eva, eva.transform.forward);
					break;
				case Command.Backward:
					//this.eva_tgtRpos.SetValue(eva, -eva.transform.forward);
					//this.eva_packTgtRPos.SetValue(eva, -eva.transform.forward);
					//this.eva_Vtgt.SetValue(eva, -eva.transform.forward);
					eva.part.Rigidbody.AddForce(-eva.transform.forward * dtime * 2f, ForceMode.Force);
					//FlightInputHandler.state.mainThrottle = 1.0f;
					break;
				case Command.Left:
					// this.eva_packTgtRPos.SetValue(eva, -eva.transform.right);
					eva.part.Rigidbody.AddForce(-eva.transform.right * dtime * 2f, ForceMode.Force);
					break;
				case Command.Right:
					this.eva_packTgtRPos.SetValue(eva, eva.transform.right);
					eva.part.Rigidbody.AddForce(eva.transform.right * dtime * 2f, ForceMode.Force);
					break;
				case Command.Up:
					this.eva_packTgtRPos.SetValue(eva, eva.transform.up);
					eva.part.Rigidbody.AddForce(eva.transform.up * dtime * 2f, ForceMode.Force);
					break;
				case Command.Down:
					this.eva_packTgtRPos.SetValue(eva, -eva.transform.up);
					eva.part.Rigidbody.AddForce(-eva.transform.up * dtime * 2f, ForceMode.Force);
					break;
				case Command.LookAt:
					if (Vector3d.Angle(eva.vessel.transform.forward,  lookdirection) < 3)
					{
						order = Command.Stop;
						break;
					}
				   // var step = eva.turnRate * dtime;
					var step = eva.rotPower * dtime;
				   // Quaternion from = eva.vessel.transform.rotation;
				   // Quaternion to = Quaternion.LookRotation((Vector3)lookdirection.normalized, eva.vessel.transform.up);
				   // Quaternion result = Quaternion.RotateTowards(from, to, step);
				  //  this.eva_tgtFwd.SetValue(eva, result * (Vector3)this.eva_tgtFwd.GetValue(eva));
				  //  this.eva_tgtUp.SetValue(eva, result * (Vector3)this.eva_tgtUp.GetValue(eva));
					   this.eva_tgtFwd.SetValue(eva, (Vector3)lookdirection.normalized);
					break;
				case Command.Stop:
					break;
				default:
					break;
			}
		}

		private void PlayAnimation(string name)
		{
			Animation _kerbalanimation = null;
			eva.vessel.GetComponentCached<Animation>(ref _kerbalanimation);
			_kerbalanimation.CrossFade(name, 0.3f, PlayMode.StopAll);
			currentanimation = name;
		}

		private void StopAllAnimations()
		{
			Animation _kerbalanimation = null;
			eva.vessel.GetComponentCached<Animation>(ref _kerbalanimation);
		   // _kerbalanimation.Stop();
			_kerbalanimation.CrossFade("idle", 0.3f, PlayMode.StopAll);

		}
		#endregion



	}
}

#endif