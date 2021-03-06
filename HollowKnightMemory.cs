﻿using LiveSplit.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
namespace LiveSplit.HollowKnight {
	public partial class HollowKnightMemory {
		private ProgramPointer gameManager, playmakerFSM;
		public Process Program { get; set; }
		public bool IsHooked { get; set; } = false;
		private DateTime lastHooked;
		private int uiManager, inputHandler, cameraCtrl, gameState, heroController, camTarget, camMode, menuState, uiState;
		private int geoCounter, heroAccepting, actorState, transistionState, camTeleport, playerData, debugInfo;
		private Version lastVersion;

		public HollowKnightMemory() {
			lastHooked = DateTime.MinValue;
			gameManager = new ProgramPointer(AutoDeref.Single,
				new ProgramSignature(PointerVersion.Normal, "83C41083EC0C57E8????????83C410EB3D8B05", 19),
				new ProgramSignature(PointerVersion.API, "83C41083EC0C57393FE8????????83C410EB3F8B05", 21)
			) { UpdatedPointer = UpdatedPointer };
			playmakerFSM = new ProgramPointer(AutoDeref.Single, new ProgramSignature(PointerVersion.Normal, "558BEC5783EC048B7D088B05????????83EC0857503900E8????????83C4108B470C85C074238B470C8BC83909", 12));
		}

		public string VersionNumber() {
			return lastVersion.ToString();
		}
		private void UpdatedPointer(ProgramPointer pointer) {
			if (pointer == gameManager) {
				//GameManager
				playerData = 0x30;
				uiManager = 0x84;
				inputHandler = 0x68;
				cameraCtrl = 0x74;
				gameState = 0x98;
				heroController = 0x78;
				debugInfo = 0x2c;

				//CameraController
				camTarget = 0x28;
				camMode = 0x40;
				camTeleport = 0x4b;

				//HeroController
				heroAccepting = 0x457;
				actorState = 0x374;
				transistionState = 0x37c;
				geoCounter = 0x1dc;

				int len = gameManager.Read<int>(Program, 0x0, inputHandler, debugInfo, 0x1c, 0x8);
				string version = null;

				if (len != 7) {
					inputHandler = 0x6c;
					uiManager = 0x88;
					cameraCtrl = 0x78;
					gameState = 0x9c;
					heroController = 0x7c;
					camTarget = 0x24;
					camMode = 0x3c;
					camTeleport = 0x47;

					len = gameManager.Read<int>(Program, 0x0, inputHandler, debugInfo, 0x1c, 0x8);
					if (len != 7) {
						playerData = 0x60;
						uiManager = 0x4c;
						inputHandler = 0x20;
						cameraCtrl = 0x3c;
						gameState = 0xb4;
						heroController = 0x40;
						debugInfo = 0x30;

						camMode = 0x38;
						camTeleport = 0x43;

						heroAccepting = 0x497;
						actorState = 0x3b0;
						transistionState = 0x3b8;
						geoCounter = 0x120;

						uiState = 0x148;
						menuState = 0x14c;

						do {
							version = gameManager.Read(Program, 0x0, inputHandler, debugInfo, 0x1c);
							if (string.IsNullOrEmpty(version)) {
								Thread.Sleep(50);
							}
						} while (string.IsNullOrEmpty(version) && len-- > 0);

						lastVersion = new Version(version);

						if (lastVersion.Minor > 3) {
							gameState = 0xb8;

							heroAccepting = 0x4b3;
							actorState = 0x3cc;
							transistionState = 0x3d4;

							uiState = 0x154;
							menuState = 0x158;
						}
					} else {
						do {
							version = gameManager.Read(Program, 0x0, inputHandler, debugInfo, 0x1c);
							if (string.IsNullOrEmpty(version)) {
								Thread.Sleep(50);
							}
						} while (string.IsNullOrEmpty(version) && len-- > 0);

						lastVersion = new Version(version);
						geoCounter = lastVersion.Build > 0 ? 0x1dc : 0x1d4;

						if (lastVersion.Minor == 0 && (lastVersion.Build < 3 || lastVersion.Revision < 4)) {
							uiState = 0x128;
							menuState = 0x12c;
						} else if (lastVersion.Minor == 0) {
							uiState = 0x12c;
							menuState = 0x130;
						} else if (lastVersion.Minor == 1) {
							uiState = 0x130;
							menuState = 0x134;
							heroAccepting = 0x45b;
							actorState = 0x378;
							transistionState = 0x380;
						} else {
							uiState = 0x130;
							menuState = 0x134;
							uiManager = 0x8c;
							cameraCtrl = 0x7c;
							gameState = 0xa0;
							heroController = 0x80;
							heroAccepting = 0x46b;
							actorState = 0x388;
							transistionState = 0x390;
							geoCounter = 0x1e4;
						}
					}
				} else {
					len = 40;
					do {
						version = gameManager.Read(Program, 0x0, inputHandler, debugInfo, 0x1c);
						if (string.IsNullOrEmpty(version)) {
							Thread.Sleep(50);
						}
					} while (string.IsNullOrEmpty(version) && len-- > 0);

					lastVersion = new Version(version);

					geoCounter = lastVersion.Build > 0 ? 0x1dc : 0x1d4;
					menuState = 0x128;
					uiState = 0x124;
				}

				HollowKnight.PlayerData.InitializeData(lastVersion);
			}
		}
		public byte[] GetPlayerData(int length) {
			//GameManger._instance.playerData
			return gameManager.ReadBytes(Program, length, 0x0, playerData, 0x0);
		}
		public void SetCameraZoom(float zoom) {
			//GameManger._instance.gameCams.tk2dCam.zoomFactor
			if (lastVersion.Minor == 3) {
				gameManager.Write<float>(Program, zoom, 0x0, 0x24, 0x48, 0x48);
			}
			gameManager.Write<float>(Program, zoom, 0x0, 0x20, 0x40, 0x48);
		}
		public bool CameraTeleporting() {
			//GameManger._instance.cameraCtrl.teleporting
			return gameManager.Read<bool>(Program, 0x0, cameraCtrl, camTeleport);
		}
		public PointF GetCameraTarget() {
			//GameManger._instance.cameraCtrl.camTarget.destination
			float x = gameManager.Read<float>(Program, 0x0, cameraCtrl, camTarget, 0x24);
			float y = gameManager.Read<float>(Program, 0x0, cameraCtrl, camTarget, 0x28);
			return new PointF(x, y);
		}
		public TargetMode GetCameraTargetMode() {
			//GameManger._instance.cameraCtrl.camTarget.mode
			return (TargetMode)gameManager.Read<int>(Program, 0x0, cameraCtrl, camTarget, 0x20);
		}
		public void SetCameraTargetMode(TargetMode mode) {
			//GameManger._instance.cameraCtrl.camTarget.mode
			gameManager.Write(Program, (int)mode, 0x0, cameraCtrl, camTarget, 0x20);
		}
		public CameraMode CameraMode() {
			//GameManager._instance.cameraCtrl.mode
			return (CameraMode)gameManager.Read<int>(Program, 0x0, cameraCtrl, camMode);
		}
		public void UpdateGeoCounter(bool enable, int geo) {
			//GameManger._instance.heroCtrl.geoCounter.digitChangeTimer
			gameManager.Write(Program, -0.02f, 0x0, heroController, geoCounter, 0x50);
			//GameManger._instance.heroCtrl.geoCounter.changePerTick
			gameManager.Write(Program, enable ? 0 : 1, 0x0, heroController, geoCounter, 0x44);
			//GameManger._instance.heroCtrl.geoCounter.addCounter
			gameManager.Write(Program, 1, 0x0, heroController, geoCounter, 0x34);
			//GameManger._instance.heroCtrl.geoCounter.counterCurrent
			gameManager.Write(Program, geo, 0x0, heroController, geoCounter, 0x2c);
			//GameManger._instance.heroCtrl.geoCounter.addRollerState
			gameManager.Write(Program, 2, 0x0, heroController, geoCounter, 0x3c);
		}
		public void EnableDebug(bool enable) {
			//inputHandler.onScreenDebugInfo.showFPS
			gameManager.Write(Program, enable, 0x0, inputHandler, debugInfo, 0x7c);
			//inputHandler.onScreenDebugInfo.showInfo
			gameManager.Write(Program, enable, 0x0, inputHandler, debugInfo, 0x7d);
			//inputHandler.onScreenDebugInfo.showInput
			gameManager.Write(Program, enable, 0x0, inputHandler, debugInfo, 0x7e);
			//inputHandler.onScreenDebugInfo.showLoadingTime
			gameManager.Write(Program, enable, 0x0, inputHandler, debugInfo, 0x7f);
			//inputHandler.onScreenDebugInfo.showTFR
			gameManager.Write(Program, enable, 0x0, inputHandler, debugInfo, 0x80);
		}
		public void SetPlayerData(Offset offset, int value) {
			//GameManger._instance.playerData.(offset)
			gameManager.Write(Program, value, 0x0, playerData, HollowKnight.PlayerData.GetOffset(offset));
		}
		public void SetPlayerData(Offset offset, bool value) {
			//GameManger._instance.playerData.(offset)
			gameManager.Write(Program, value, 0x0, playerData, HollowKnight.PlayerData.GetOffset(offset));
		}
		public List<EnemyInfo> GetEnemyInfo() {
			List<EnemyInfo> enemies = new List<EnemyInfo>();
			int size = playmakerFSM.Read<int>(Program, 0x0, 0xc);
			IntPtr basePointer = (IntPtr)playmakerFSM.Read<uint>(Program, 0x0, 0x8);
			for (int x = 0; x < size; x++) {
				IntPtr fsmPtr = (IntPtr)Program.Read<uint>(basePointer, 0x10 + x * 4, 0xc);
				if (fsmPtr == IntPtr.Zero) { continue; }
				int fsmLength = Program.Read<int>(fsmPtr, 0x14, 0x8);
				byte fsmChar = Program.Read<byte>(fsmPtr, 0x14, 0xc);
				if (fsmLength != 20 || fsmChar != (byte)'h') { continue; }

				EnemyInfo info = new EnemyInfo();
				info.Pointer = Program.Read<uint>(fsmPtr, 0x28, 0xc);

				int infoSize = Program.Read<int>((IntPtr)info.Pointer, 0xc);
				if (infoSize == 0) { continue; }

				for (int i = 0; i < infoSize; i++) {
					fsmLength = Program.Read<int>((IntPtr)info.Pointer, 0x10 + i * 4, 0x8, 0x8);
					fsmChar = Program.Read<byte>((IntPtr)info.Pointer, 0x10 + i * 4, 0x8, 0xc);
					if (fsmLength != 2 || fsmChar != (byte)'H') { continue; }

					info.HPIndex = i;
					info.HP = Program.Read<int>((IntPtr)info.Pointer, 0x10 + i * 4, 0x14);
				}

				enemies.Add(info);
			}

			return enemies;
		}
		public List<EntityInfo> GetEntityInfo() {
			List<EntityInfo> entities = new List<EntityInfo>();
			int size = playmakerFSM.Read<int>(Program, 0x0, 0xc);
			for (int x = 0; x < size; x++) {
				IntPtr fsmPtr = (IntPtr)playmakerFSM.Read<uint>(Program, 0x0, 0x8, 0x10 + x * 4, 0xc);
				if (fsmPtr == IntPtr.Zero) { continue; }
				string fsm = Program.Read((IntPtr)Program.Read<uint>(fsmPtr, 0x14));

				EntityInfo info = new EntityInfo();
				info.Name = fsm;
				info.Pointer = Program.Read<uint>(fsmPtr, 0x28);

				for (int j = 0x8; j <= 0x30; j += 4) {
					int infoSize = Program.Read<int>((IntPtr)info.Pointer, j, 0xc);
					if (infoSize == 0) { continue; }

					for (int i = 0; i < infoSize; i++) {
						string fsmName = Program.Read((IntPtr)Program.Read<uint>((IntPtr)info.Pointer, j, 0x10 + i * 4, 0x8));
						if (string.IsNullOrEmpty(fsmName)) { continue; }

						switch (j) {
							case 0x8: info.FloatVars.Add(new KeyValuePair<string, float>(fsmName, Program.Read<float>((IntPtr)info.Pointer, j, 0x10 + i * 4, 0x14))); break;
							case 0xc: info.IntVars.Add(new KeyValuePair<string, int>(fsmName, Program.Read<int>((IntPtr)info.Pointer, j, 0x10 + i * 4, 0x14))); break;
							case 0x10: info.BoolVars.Add(new KeyValuePair<string, bool>(fsmName, Program.Read<bool>((IntPtr)info.Pointer, j, 0x10 + i * 4, 0x14))); break;
							case 0x14: info.StringVars.Add(new KeyValuePair<string, string>(fsmName, Program.Read((IntPtr)Program.Read<int>((IntPtr)info.Pointer, j, 0x10 + i * 4, 0x14)))); break;
							case 0x18:
							case 0x1c: info.VectorVars.Add(new KeyValuePair<string, PointF>(fsmName, new PointF(Program.Read<float>((IntPtr)info.Pointer, j, 0x10 + i * 4, 0x14), Program.Read<float>((IntPtr)info.Pointer, j, 0x10 + i * 4, 0x18)))); break;
							default: info.ObjVars.Add(new KeyValuePair<string, int>(fsmName, Program.Read<int>((IntPtr)info.Pointer, j, 0x10 + i * 4, 0x14))); break;
						}
					}
				}

				if (info.Count > 0) {
					entities.Add(info);
				}
			}

			return entities;
		}
		public T PlayerData<T>(Offset offset) where T : struct {
			//GameManger._instance.playerData.(offset)
			switch (offset) {
				case Offset.bossDoorStateTier1:
				case Offset.bossDoorStateTier2:
				case Offset.bossDoorStateTier3:
				case Offset.bossDoorStateTier4:
				case Offset.bossDoorStateTier5:
					return gameManager.Read<T>(Program, 0x0, playerData, HollowKnight.PlayerData.GetOffset(offset), 0xa);
				default:
					return gameManager.Read<T>(Program, 0x0, playerData, HollowKnight.PlayerData.GetOffset(offset));
			}
		}
		public GameState GameState() {
			//GameManager._instance.gameState
			return (GameState)gameManager.Read<int>(Program, 0x0, gameState);
		}
		public MainMenuState MenuState() {
			//GameManager._instance.uiManager.menuState
			return (MainMenuState)gameManager.Read<int>(Program, 0x0, uiManager, menuState);
		}
		public UIState UIState() {
			//GameManager._instance.uiManager.uiState
			int ui = gameManager.Read<int>(Program, 0x0, uiManager, uiState);
			if (uiState != 0x124 && ui >= 2) {
				ui += 2;
			}
			return (UIState)ui;
		}
		public bool AcceptingInput() {
			//GameManager._instance.InputHandler.acceptingInput
			if (lastVersion.Minor >= 3) {
				return gameManager.Read<bool>(Program, 0x0, inputHandler, 0x5c);
			}
			return gameManager.Read<bool>(Program, 0x0, inputHandler, 0x58);
		}
		public bool AcceptingInputHero() {
			//GameManager._instance.heroCtrl.acceptingInput
			return gameManager.Read<bool>(Program, 0x0, heroController, heroAccepting);
		}
		public ActorStates HeroActorState() {
			//GameManager._instance.heroCtrl.actor_state
			return (ActorStates)gameManager.Read<int>(Program, 0x0, heroController, actorState);
		}
		public HeroTransitionState HeroTransitionState() {
			//GameManager._instance.heroCtrl.transitionState
			return (HeroTransitionState)gameManager.Read<int>(Program, 0x0, heroController, transistionState);
		}
		public string SceneName() {
			//GameManager._instance.sceneName
			return gameManager.Read(Program, 0x0, 0xc);
		}
		public string NextSceneName() {
			//GameManager._instance.nextSceneName
			return gameManager.Read(Program, 0x0, 0x10);
		}
		public bool HookProcess() {
			IsHooked = Program != null && !Program.HasExited;
			if (!IsHooked && DateTime.Now > lastHooked.AddSeconds(1)) {
				lastHooked = DateTime.Now;
				Process[] processes = Process.GetProcessesByName("Hollow_Knight");
				Program = processes.Length == 0 ? null : processes[0];
				if (Program != null && !Program.HasExited) {
					MemoryReader.Update64Bit(Program);
					IsHooked = true;
				}
			}

			if (!IsHooked) {
				lastVersion = null;
			}

			return IsHooked;
		}
		public void Dispose() {
			if (Program != null) {
				Program.Dispose();
			}
		}
	}
	public enum PointerVersion {
		Normal,
		API
	}
	public enum AutoDeref {
		None,
		Single,
		Double
	}
	public class ProgramSignature {
		public PointerVersion Version { get; set; }
		public string Signature { get; set; }
		public int Offset { get; set; }
		public ProgramSignature(PointerVersion version, string signature, int offset) {
			Version = version;
			Signature = signature;
			Offset = offset;
		}
		public override string ToString() {
			return Version.ToString() + " - " + Signature;
		}
	}
	public class ProgramPointer {
		private int lastID;
		private DateTime lastTry;
		private ProgramSignature[] signatures;
		private int[] offsets;
		public IntPtr Pointer { get; private set; }
		public PointerVersion Version { get; private set; }
		public AutoDeref AutoDeref { get; private set; }
		public Action<ProgramPointer> UpdatedPointer { get; set; }

		public ProgramPointer(AutoDeref autoDeref, params ProgramSignature[] signatures) {
			AutoDeref = autoDeref;
			this.signatures = signatures;
			lastID = -1;
			lastTry = DateTime.MinValue;
		}
		public ProgramPointer(AutoDeref autoDeref, params int[] offsets) {
			AutoDeref = autoDeref;
			this.offsets = offsets;
			lastID = -1;
			lastTry = DateTime.MinValue;
		}

		public T Read<T>(Process program, params int[] offsets) where T : struct {
			GetPointer(program);
			return program.Read<T>(Pointer, offsets);
		}
		public string Read(Process program, params int[] offsets) {
			GetPointer(program);
			return program.Read((IntPtr)program.Read<uint>(Pointer, offsets));
		}
		public byte[] ReadBytes(Process program, int length, params int[] offsets) {
			GetPointer(program);
			return program.Read(Pointer, length, offsets);
		}
		public void Write<T>(Process program, T value, params int[] offsets) where T : struct {
			GetPointer(program);
			program.Write<T>(Pointer, value, offsets);
		}
		public void Write(Process program, byte[] value, params int[] offsets) {
			GetPointer(program);
			program.Write(Pointer, value, offsets);
		}
		public IntPtr GetPointer(Process program) {
			if (program == null) {
				Pointer = IntPtr.Zero;
				lastID = -1;
				return Pointer;
			} else if (program.Id != lastID) {
				Pointer = IntPtr.Zero;
				lastID = program.Id;
			}

			if (Pointer == IntPtr.Zero && DateTime.Now > lastTry.AddSeconds(1)) {
				lastTry = DateTime.Now;

				Pointer = GetVersionedFunctionPointer(program);
				if (Pointer != IntPtr.Zero) {
					if (AutoDeref != AutoDeref.None) {
						Pointer = (IntPtr)program.Read<uint>(Pointer);
						if (AutoDeref == AutoDeref.Double) {
							if (MemoryReader.is64Bit) {
								Pointer = (IntPtr)program.Read<ulong>(Pointer);
							} else {
								Pointer = (IntPtr)program.Read<uint>(Pointer);
							}
						}
					}
					UpdatedPointer?.Invoke(this);
				}
			}
			return Pointer;
		}
		private IntPtr GetVersionedFunctionPointer(Process program) {
			if (signatures != null) {
				MemorySearcher searcher = new MemorySearcher();
				searcher.MemoryFilter = delegate (MemInfo info) {
					return (info.State & 0x1000) != 0 && (info.Protect & 0x40) != 0 && (info.Protect & 0x100) == 0;
				};
				for (int i = 0; i < signatures.Length; i++) {
					ProgramSignature signature = signatures[i];

					IntPtr ptr = searcher.FindSignature(program, signature.Signature);
					if (ptr != IntPtr.Zero) {
						Version = signature.Version;
						return ptr + signature.Offset;
					}
				}
			} else {
				IntPtr ptr = (IntPtr)program.Read<uint>(program.MainModule.BaseAddress, offsets);
				if (ptr != IntPtr.Zero) {
					return ptr;
				}
			}

			return IntPtr.Zero;
		}
	}

	public class PlayerData {
		public static Dictionary<string, PlayerKey> Data = new Dictionary<string, PlayerKey>(StringComparer.OrdinalIgnoreCase);
		public static int DataLength;

		public PlayerData() { }

		public static void InitializeData(Version ver) {
			Assembly asm = Assembly.GetExecutingAssembly();

			Stream file = asm.GetManifestResourceStream("LiveSplit.HollowKnight.PlayerData.V1424.txt");
			if (ver.Minor == 0 && (ver.Build < 3 || ver.Revision < 2)) {
				file = asm.GetManifestResourceStream("LiveSplit.HollowKnight.PlayerData.Original.txt");
			} else if (ver.Minor == 0) {
				file = asm.GetManifestResourceStream("LiveSplit.HollowKnight.PlayerData.V1032.txt");
			} else if (ver.Minor == 1) {
				file = asm.GetManifestResourceStream("LiveSplit.HollowKnight.PlayerData.V1114.txt");
			} else if (ver.Minor == 2 && ver.Build == 1 && ver.Revision < 4) {
				file = asm.GetManifestResourceStream("LiveSplit.HollowKnight.PlayerData.V1211.txt");
			} else if (ver.Minor == 2 && ((ver.Build == 1 && ver.Revision >= 4) || ver.Build > 1)) {
				file = asm.GetManifestResourceStream("LiveSplit.HollowKnight.PlayerData.V1214.txt");
			} else if (ver.Minor == 3) {
				file = asm.GetManifestResourceStream("LiveSplit.HollowKnight.PlayerData.V1315.txt");
			}

			if (file != null) {
				Data.Clear();

				string line = null;
				DataLength = 0;
				using (StreamReader sr = new StreamReader(file)) {
					while ((line = sr.ReadLine()) != null) {
						string[] record = line.Split(':');
						int offset = int.Parse(record[0], NumberStyles.HexNumber);
						Data.Add(record[1], new PlayerKey(offset, record[1], record[2]));
						if (offset > DataLength) { DataLength = offset; }
					}
				}
				DataLength++;
			}
		}

		public static int GetOffset(Offset offset) {
			PlayerKey key = null;
			if (Data.TryGetValue(offset.ToString(), out key)) {
				return key.Index;
			}
			return 0;
		}
		public void UpdateData(HollowKnightMemory mem, Action<string> logWriter) {
			Process program = mem.Program;
			byte[] playerData = mem.GetPlayerData(DataLength);
			foreach (KeyValuePair<string, PlayerKey> pair in Data) {
				PlayerKey key = pair.Value;

				switch (key.Name) {
					case "scenesVisited":
					case "scenesMapped":
					case "scenesEncounteredBench":
					case "scenesGrubRescued":
					case "scenesEncounteredCocoon":
					case "scenesEncounteredDreamPlant":
					case "scenesEncounteredDreamPlantC":
					case "playerStory":
					case "MPCharge":
					case "MPReserve":
					case "equippedCharms": continue;
				}

				object oldValue = key.Value;

				switch (key.Type) {
					case "Vector3": key.Value = new PointF(BitConverter.ToSingle(playerData, key.Index), BitConverter.ToSingle(playerData, key.Index + 4)); break;
					case "Single": key.Value = BitConverter.ToSingle(playerData, key.Index); break;
					case "Int16": key.Value = BitConverter.ToInt16(playerData, key.Index); break;
					case "Int64": key.Value = BitConverter.ToInt64(playerData, key.Index); break;
					case "String": key.Value = program.Read((IntPtr)BitConverter.ToUInt32(playerData, key.Index)); break;
					case "Completion": key.Value = program.Read<bool>((IntPtr)BitConverter.ToUInt32(playerData, key.Index), 0xa); break;
					case "Byte": key.Value = playerData[key.Index]; break;
					case "Boolean": key.Value = playerData[key.Index] == 1; break;
					default: key.Value = BitConverter.ToInt32(playerData, key.Index); break;
				}

				bool changed = oldValue == null;
				if (!changed) {
					switch (key.Type) {
						case "Vector3": changed = (PointF)oldValue != (PointF)key.Value; break;
						case "Single": changed = (float)oldValue != (float)key.Value; break;
						case "Int16": changed = (short)oldValue != (short)key.Value; break;
						case "Int64": changed = (long)oldValue != (long)key.Value; break;
						case "String": changed = (string)oldValue != (string)key.Value; break;
						case "Completion": changed = (bool)oldValue != (bool)key.Value; break;
						case "Byte": changed = (byte)oldValue != (byte)key.Value; break;
						case "Boolean": changed = (bool)oldValue != (bool)key.Value; break;
						default: changed = (int)oldValue != (int)key.Value; break;
					}
				}
				if (changed && oldValue != null && logWriter != null) {
					logWriter(key.ToString(oldValue));
				}
			}
		}
	}
	public class PlayerKey {
		public int Index { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
		public object Value { get; set; }

		public PlayerKey(int index, string name, string type) {
			Index = index;
			Name = name;
			Type = type;
		}

		public override string ToString() {
			return Index.ToString("X") + " " + Name + " " + Type + " = " + (Value ?? "").ToString();
		}
		public string ToString(object oldValue) {
			return Name + ": " + (oldValue ?? "").ToString() + " -> " + (Value ?? "").ToString();
		}
	}
}