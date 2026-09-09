# TheSuperUninstaller 🚀

A clean, lightweight, professional, and portable native Windows desktop utility built in **F#** and **Windows Forms**, designed for efficient software management and uninstallation.

## ✨ Key Features
* **Deep Registry Scanning:** Detects installed software across 64-bit and 32-bit `LocalMachine` as well as `CurrentUser` registry hives.
* **Lightning-Fast Filtering:** Interactive real-time search box to instantly find applications by name.
* **Smart Uninstallation:** Supports standard uninstall strings and automatically handles `msiexec.exe` commands.
* **Built-in Protection:** Features a hardcoded "Easter Egg" logic that prevents the application from attempting to uninstall itself.
* **100% Portable:** Compiles into a single, self-contained, and compressed executable with zero external runtime dependencies.

## 🛠️ Tech Stack
* **Language:** F# (.NET 10.0-windows)
* **UI Framework:** Windows Forms (WinForms)

## 📦 Building & Publishing
To build a standalone, single-file, and compressed executable for distribution, run the following command in your terminal:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

⚠️ Disclaimer
This tool reads system registry keys to list installed programs. Machine-wide software detection relies on standard Windows registry structures. Use responsibly!