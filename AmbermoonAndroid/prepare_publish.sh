SILK_JNI="$(cygpath "$USERPROFILE")/.nuget/packages/silk.net.windowing.sdl/2.23.0/lib/net7.0-android33.0/Silk.NET.Windowing.Sdl/jni"
ULTZ="$(cygpath "$USERPROFILE")/.nuget/packages/ultz.native.sdl/2.32.10/runtimes"

# Replace Silk.NET lib files (SDL and main aka SilkDroid)
cp libs/arm64-v8a/libmain.so "$SILK_JNI/arm64-v8a/libmain.so"
cp libs/arm64-v8a/libSDL2.so "$SILK_JNI/arm64-v8a/libSDL2.so"
cp libs/x86_64/libmain.so "$SILK_JNI/x86_64/libmain.so"
cp libs/x86_64/libSDL2.so "$SILK_JNI/x86_64/libSDL2.so"

# Replace Ultz SDL lib files (called libSDL2-2.0.so there)
cp libs/arm64-v8a/libSDL2.so "$ULTZ/linux-arm64/native/libSDL2-2.0.so"
cp libs/x86_64/libSDL2.so "$ULTZ/linux-x64/native/libSDL2-2.0.so"

dotnet clean