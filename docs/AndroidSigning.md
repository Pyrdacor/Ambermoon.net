# Android release signing in AppVeyor

Builds of `android-v*` tags sign the Android packages with the release keystore, so the
`com.pyrdacor.AmbermoonAndroid-Signed.aab` artifact can be uploaded to the Google Play Store.

The keystore is committed only in encrypted form (`AmbermoonAndroid/ambermoon.keystore.enc`).
The key to decrypt it and the keystore password are secure environment variables of the AppVeyor
project. They are not in the repository and are not passed to pull request builds from forks.
Without them the build still works but uses the debug key (with a warning in the log).

## One-time setup

1. Encrypt the keystore with AppVeyor's `secure-file` tool (Windows, PowerShell, in the repository root).
   Choose a long random secret, it is only needed by AppVeyor:

   ```
   iex ((New-Object Net.WebClient).DownloadString('https://raw.githubusercontent.com/appveyor/secure-file/master/install.ps1'))
   appveyor-tools\secure-file.exe -encrypt <path to ambermoon.keystore> -secret <secret> -out AmbermoonAndroid\ambermoon.keystore.enc
   ```

   The tool prints the salt (`Salt: ...`). Keep the secret and the salt. Call `secure-file.exe` with the
   extension: `appveyor-tools` also contains an extensionless bash script `secure-file` for Linux. If Windows
   starts that one instead, the output appears in another window which closes immediately.

2. In AppVeyor, open the project settings, tab "Environment", and add these variables. Use the lock
   icon to make each of them secure (hidden in the UI and the log):

   | Variable | Value |
   |---|---|
   | `KEYSTORE_SECRET` | the secret of step 1 |
   | `KEYSTORE_SALT` | the salt printed in step 1 |
   | `ANDROID_KEYSTORE_PASS` | the keystore password (store and key password are the same, alias `ambermoon`) |

3. Commit `AmbermoonAndroid/ambermoon.keystore.enc`. Never commit the keystore itself or the
   password file, `.gitignore` excludes `*.keystore` and `*.keystore.pass.txt`.

## Release

Push a tag like `android-v1.14.0`. The build creates:

- `com.pyrdacor.AmbermoonAndroid-Signed.aab`: app bundle for the Play Store (AppVeyor artifact, upload it manually)
- `com.pyrdacor.AmbermoonAndroid-Signed.apk`: signed APK, deployed to the GitHub release draft

## Upload key

If the app uses Play App Signing, the keystore is only the upload key. A lost or exposed upload key can be
reset in the Play Console (Setup > App signing > Request upload key reset) with a new keystore.
