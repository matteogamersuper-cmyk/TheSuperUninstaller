namespace TheSuperUninstaller

open System
open System.Drawing
open System.Windows.Forms
open Microsoft.Win32
open System.Diagnostics

type AppInfo = {
    Name : string
    Version : string
    Source : string
    UninstallString : string
}

type MainForm() as this =
    inherit Form()

    let mutable appListView : ListView = null
    let mutable txtSearch : TextBox = null
    let mutable btnScan : Button = null
    let mutable btnUninstall : Button = null
    let mutable btnRefresh : Button = null
    let mutable lblStatus : Label = null

    let mutable allApps : AppInfo list = []

    do
        let _ = this.Text <- "TheSuperUninstaller"
        let _ = this.Size <- Size(880, 600)
        let _ = this.StartPosition <- FormStartPosition.CenterScreen
        let _ = this.FormBorderStyle <- FormBorderStyle.FixedSingle
        let _ = this.MaximizeBox <- false

        this.InitUI()

    member private this.InitUI() =
        let lblSearch = new Label(Text = "🔍 Cerca:", Location = Point(12, 15), Size = Size(50, 23))
        let _ = this.Controls.Add(lblSearch)

        txtSearch <- new TextBox(Location = Point(65, 12), Size = Size(547, 23))
        let _ = txtSearch.TextChanged.Add(fun _ -> this.FilterApps(txtSearch.Text))
        let _ = this.Controls.Add(txtSearch)

        appListView <- new ListView(
            Location = Point(12, 45),
            Size = Size(600, 480),
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        )
        let _ = appListView.Columns.Add("Nome Applicazione", 340, HorizontalAlignment.Left)
        let _ = appListView.Columns.Add("Versione", 100, HorizontalAlignment.Left)
        let _ = appListView.Columns.Add("Origine", 140, HorizontalAlignment.Left)
        let _ = this.Controls.Add(appListView)

        btnScan <- new Button(
            Text = "🔍 Scansiona Sistema",
            Location = Point(630, 45),
            Size = Size(220, 45),
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10.0f, FontStyle.Bold)
        )
        let _ = btnScan.Click.Add(fun _ -> this.OnScanClick())
        let _ = this.Controls.Add(btnScan)

        btnUninstall <- new Button(
            Text = "💥 Disinstalla Selezionato",
            Location = Point(630, 105),
            Size = Size(220, 45),
            BackColor = Color.FromArgb(192, 57, 43),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.0f, FontStyle.Bold)
        )
        let _ = btnUninstall.Click.Add(fun _ -> this.OnUninstallClick())
        let _ = this.Controls.Add(btnUninstall)

        btnRefresh <- new Button(
            Text = "🔄 Aggiorna Lista",
            Location = Point(630, 165),
            Size = Size(220, 35),
            FlatStyle = FlatStyle.Flat
        )
        let _ = btnRefresh.Click.Add(fun _ ->
            let _ = txtSearch.Text <- ""
            this.OnScanClick()
        )
        let _ = this.Controls.Add(btnRefresh)

        lblStatus <- new Label(
            Text = "Pronto alla scansione...",
            Location = Point(12, 535),
            Size = Size(600, 25),
            Font = new Font("Segoe UI", 9.0f, FontStyle.Italic)
        )
        let _ = this.Controls.Add(lblStatus)
        ()

    member private this.ScanRegistryHive(baseKey: RegistryKey, subKeyPath: string, sourceName: string, itemsList: System.Collections.Generic.List<AppInfo>) =
        try
            use key = baseKey.OpenSubKey(subKeyPath)
            let hasKey = (key <> null) |> box :?> bool
            if hasKey then
                for subKeyName in key.GetSubKeyNames() do
                    use subKey = key.OpenSubKey(subKeyName)
                    let hasSubKey = (subKey <> null) |> box :?> bool
                    if hasSubKey then
                        let displayName = subKey.GetValue("DisplayName") :?> string
                        let hasName = (not (String.IsNullOrEmpty(displayName))) |> box :?> bool
                        if hasName then
                            let displayVersion =
                                match subKey.GetValue("DisplayVersion") with
                                | :? string as v -> v
                                | _ -> "N/D"

                            let uninstallString =
                                match subKey.GetValue("UninstallString") with
                                | :? string as u -> u
                                | _ -> ""

                            let hasUninstall = (not (String.IsNullOrEmpty(uninstallString))) |> box :?> bool
                            if hasUninstall then
                                let _ = itemsList.Add({ Name = displayName; Version = displayVersion; Source = sourceName; UninstallString = uninstallString })
                                ()
                            else
                                ()
                        else
                            ()
                    else
                        ()
            else
                ()
        with
        | _ -> ()

    member private this.PopulateListView(apps: AppInfo list) =
        let _ = appListView.Items.Clear()
        for app in apps do
            let item = new ListViewItem(app.Name)
            let _ = item.SubItems.Add(app.Version)
            let _ = item.SubItems.Add(app.Source)
            let _ = item.Tag <- app
            let _ = appListView.Items.Add(item)
            ()
        ()

    member private this.FilterApps(query: string) =
        let isQueryEmpty = (String.IsNullOrWhiteSpace(query)) |> box :?> bool
        let filtered =
            if isQueryEmpty then allApps
            else allApps |> List.filter (fun app -> (app.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) |> box :?> bool)
        let _ = this.PopulateListView(filtered)
        let _ = lblStatus.Text <- sprintf "Visualizzati %d elementi (filtrati)." appListView.Items.Count
        ()

    member private this.OnScanClick() =
        let _ = lblStatus.Text <- "Scansione del registro di Windows in corso..."

        let foundApps = new System.Collections.Generic.List<AppInfo>()

        let _ = foundApps.Add({ Name = "TheSuperUninstaller"; Version = "1.0.0"; Source = "Self"; UninstallString = "SELF" })

        let uninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
        let _ = this.ScanRegistryHive(Registry.LocalMachine, uninstallPath, "Registry (Machine 64)", foundApps)
        let _ = this.ScanRegistryHive(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", "Registry (Machine 32)", foundApps)
        let _ = this.ScanRegistryHive(Registry.CurrentUser, uninstallPath, "Registry (User)", foundApps)

        let _ = allApps <- foundApps |> Seq.toList |> List.distinctBy (fun x -> x.Name)

        let _ = this.PopulateListView(allApps)
        let _ = lblStatus.Text <- sprintf "Scansione completata. Trovati %d programmi installati." allApps.Length
        ()

    member private this.OnUninstallClick() =
        let hasSelection = (appListView.SelectedItems.Count > 0) |> box :?> bool
        if hasSelection then
            let selectedItem = appListView.SelectedItems.[0]
            let appInfo = selectedItem.Tag :?> AppInfo

            let isSelf = (appInfo.Name.Contains("TheSuperUninstaller", StringComparison.OrdinalIgnoreCase)) |> box :?> bool
            if isSelf then
                let _ = MessageBox.Show("Di certo non puoi disinstallare questa applicazione mentre è aperta!", "TheSuperUninstaller", MessageBoxButtons.OK, MessageBoxIcon.Error)
                ()
            else
                let confirm = MessageBox.Show(sprintf "Sei sicuro di voler avviare la disinstallazione di:\n\n%s?" appInfo.Name, "Conferma Disinstallazione", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                let isConfirmed = ((confirm = DialogResult.Yes)) |> box :?> bool

                if isConfirmed then
                    try
                        let psi = new ProcessStartInfo()
                        let isMsi = (appInfo.UninstallString.Contains("msiexec.exe", StringComparison.OrdinalIgnoreCase)) |> box :?> bool

                        if isMsi then
                            let _ = psi.FileName <- "cmd.exe"
                            let _ = psi.Arguments <- sprintf "/c %s" appInfo.UninstallString
                            ()
                        else
                            let _ = psi.FileName <- appInfo.UninstallString
                            ()

                        let _ = psi.UseShellExecute <- true
                        let _ = Process.Start(psi)
                        let _ = lblStatus.Text <- sprintf "Disinstallazione avviata per: %s" appInfo.Name
                        ()
                    with
                    | ex ->
                        let _ = MessageBox.Show(sprintf "Impossibile avviare il programma di disinstallazione.\nErrore: %s" ex.Message, "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        ()
                else
                    ()
        else
            let _ = MessageBox.Show("Seleziona prima un programma dalla lista!", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Information)
            ()
        ()

module Program =
    [<EntryPoint>]
    let main args =
        let _ = Application.SetHighDpiMode(HighDpiMode.SystemAware)
        let _ = Application.EnableVisualStyles()
        let _ = Application.SetCompatibleTextRenderingDefault(false)

        use form = new MainForm()
        let _ = Application.Run(form)
        0