# KAMITSUBAKIMod — BepInEx (Mono) template with a working Update logger

## Paths (already pre-filled for you)
Edit `build/Directory.Build.props` only if your install path differs.

## Build
- Open `src/KAMITSUBAKIMod/KAMITSUBAKIMod.csproj` in Visual Studio (Release).
- Build. The dll will be copied to `BepInEx/plugins/KAMITSUBAKIMod/`.

## Test
Run the game and watch `BepInEx/LogOutput.log`. You should see:
- `KAMITSUBAKIMod 1.0.0 loaded (PatchAll ok)`
- `[KAMITSUBAKIMod] Update tick` every ~2 seconds.

## Add your first Harmony patch
- Open `Patches/Example_StringPatch.cs`.
- Replace `"GameNamespace.Player:TakeDamage"` with a real `"Namespace.Type:Method"` from your game.
- Rebuild and test.

## TargetFramework note
Keep `net472` for Unity 2018+ Mono games. For legacy (mscorlib 2.0 only) switch to `net35`.

Happy modding!
