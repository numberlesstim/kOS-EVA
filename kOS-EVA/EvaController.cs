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

		[HarmonyPatch(typeof(KerbalEVA), nameof(KerbalEVA.SetupFSM))]
		class KerbalEVA_SetupFSM_Patch
		{
			static void Postfix(KerbalEVA __instance)
			{
				var evaController = __instance.part.FindModuleImplementing<EvaController>();
				evaController.PatchStateMachine(__instance);
			}
		}
	}

	public class EvaController : PartModule
	{
		public Vector3 MovementThrottle
		{
			get => m_movementThrottle;
			set
			{
				AssumeControl();
				// TODO: does this need clamping...?
				m_movementThrottle = value;
			}
		} 

		public Vector3 LookDirection
		{
			get => m_lookDirection;
			set
			{
				AssumeControl();
				m_lookDirection = value.normalized;
			}
		}

		public bool Jump
		{
			get => m_jump;
			set
			{
				AssumeControl();
				m_jump = value;
			}
		}
		public bool Sprint
		{
			get => m_sprint;
			set
			{
				AssumeControl();
				m_sprint = value;
			}
		}

		public bool Neutralize
		{
			get { return !m_kosControl; }
			set { m_kosControl = !value; }
		}

		Vector3 m_movementThrottle;
		Vector3 m_lookDirection;
		bool m_jump;
		bool m_sprint;

		KerbalEVA m_kerbalEVA;
		bool m_kosControl;

		public override void OnStart(StartState state)
		{
			base.OnStart(state);
		
			if (!HighLogic.LoadedSceneIsFlight) return;

			m_kerbalEVA = part.FindModuleImplementing<KerbalEVA>();
		}

		internal void PatchStateMachine(KerbalEVA kerbalEVA)
		{
			m_kerbalEVA = kerbalEVA;

			m_kerbalEVA.On_jump_start.OnCheckCondition = JumpStart_CheckCondition;
			m_kerbalEVA.st_jump.OnEnter += Jump_OnEnter;

			m_kerbalEVA.On_startRun.OnCheckCondition = StartRun_CheckCondition;
			m_kerbalEVA.On_endRun.OnCheckCondition = EndRun_CheckCondition;
		}

		private void AssumeControl()
		{
			if (!m_kosControl)
			{
				m_movementThrottle = Vector3.zero;
				m_lookDirection = transform.forward;
				m_jump = false;
				m_kosControl = true;
			}
		}

		private bool EndRun_CheckCondition(KFSMState currentState)
		{
			return m_kerbalEVA.VesselUnderControl && (m_kosControl ? !Sprint : !GameSettings.EVA_Run.GetKeyDown());
		}

		private bool StartRun_CheckCondition(KFSMState currentState)
		{
			return m_kerbalEVA.VesselUnderControl && (m_kosControl ? Sprint : GameSettings.EVA_Run.GetKeyDown());
		}

		private bool JumpStart_CheckCondition(KFSMState currentState)
		{
			return m_kerbalEVA.VesselUnderControl && 
				((m_kosControl && Jump) || GameSettings.EVA_Jump.GetKeyDown()) && 
				!m_kerbalEVA.PartPlacementMode && !EVAConstructionModeController.MovementRestricted;
		}

		private void Jump_OnEnter(KFSMState s)
		{
			m_jump = false;
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

			// the movement code will not try to turn in place if tgtRPos is zero
			if (m_kerbalEVA.tgtRpos == Vector3.zero && Vector3.Dot(LookDirection, m_kerbalEVA.transform.forward) < 0.999f)
			{
				m_kerbalEVA.tgtRpos = m_lookDirection * 0.0001f;
			}

			// parachuteInput gets cleared in handleMovementInput, so we need to set it in postfix
		}
	}

}
