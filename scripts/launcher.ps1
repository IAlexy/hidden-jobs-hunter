Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

[System.Windows.Forms.Application]::EnableVisualStyles()

# Theme Colors (Modern Dark Palette)
$BgColor      = [System.Drawing.ColorTranslator]::FromHtml("#181926")
$CardBg       = [System.Drawing.ColorTranslator]::FromHtml("#24273A")
$TextColor    = [System.Drawing.ColorTranslator]::FromHtml("#CAD3F5")
$SubTextColor = [System.Drawing.ColorTranslator]::FromHtml("#A5ADCB")
$GreenColor   = [System.Drawing.ColorTranslator]::FromHtml("#A6E3A1")
$RedColor     = [System.Drawing.ColorTranslator]::FromHtml("#F38BA8")
$BlueColor    = [System.Drawing.ColorTranslator]::FromHtml("#89B4FA")
$AccentBtn    = [System.Drawing.ColorTranslator]::FromHtml("#1E66F5")
$DangerBtn    = [System.Drawing.ColorTranslator]::FromHtml("#E64553")
$SecondaryBtn = [System.Drawing.ColorTranslator]::FromHtml("#363A4F")

# Main Window
$form = New-Object System.Windows.Forms.Form
$form.Text = "HiddenJobs Hunter - Launcher"
$form.Size = New-Object System.Drawing.Size(540, 520)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedDialog
$form.MaximizeBox = $false
$form.BackColor = $BgColor
$form.ForeColor = $TextColor
$form.Font = New-Object System.Drawing.Font("Segoe UI", 9)

# Header Title
$lblTitle = New-Object System.Windows.Forms.Label
$lblTitle.Text = "HiddenJobs Hunter"
$lblTitle.Font = New-Object System.Drawing.Font("Segoe UI", 16, [System.Drawing.FontStyle]::Bold)
$lblTitle.ForeColor = [System.Drawing.Color]::White
$lblTitle.Location = New-Object System.Drawing.Point(24, 18)
$lblTitle.Size = New-Object System.Drawing.Size(476, 32)
$form.Controls.Add($lblTitle)

# Subtitle
$lblSub = New-Object System.Windows.Forms.Label
$lblSub.Text = "Marche invisible & Remote Software Engineering"
$lblSub.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Regular)
$lblSub.ForeColor = $SubTextColor
$lblSub.Location = New-Object System.Drawing.Point(26, 50)
$lblSub.Size = New-Object System.Drawing.Size(476, 20)
$form.Controls.Add($lblSub)

# Status Card Panel
$statusPanel = New-Object System.Windows.Forms.Panel
$statusPanel.Location = New-Object System.Drawing.Point(24, 80)
$statusPanel.Size = New-Object System.Drawing.Size(476, 110)
$statusPanel.BackColor = $CardBg
$form.Controls.Add($statusPanel)

# Backend Row
$lblBackName = New-Object System.Windows.Forms.Label
$lblBackName.Text = "Backend (.NET 8)"
$lblBackName.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$lblBackName.ForeColor = $TextColor
$lblBackName.Location = New-Object System.Drawing.Point(18, 16)
$lblBackName.Size = New-Object System.Drawing.Size(220, 22)
$statusPanel.Controls.Add($lblBackName)

$lblBackPort = New-Object System.Windows.Forms.Label
$lblBackPort.Text = "http://localhost:5000"
$lblBackPort.Font = New-Object System.Drawing.Font("Segoe UI", 8.5)
$lblBackPort.ForeColor = $SubTextColor
$lblBackPort.Location = New-Object System.Drawing.Point(18, 38)
$lblBackPort.Size = New-Object System.Drawing.Size(220, 18)
$statusPanel.Controls.Add($lblBackPort)

$lblBackStatus = New-Object System.Windows.Forms.Label
$lblBackStatus.Text = "Verification..."
$lblBackStatus.Font = New-Object System.Drawing.Font("Segoe UI", 9.5, [System.Drawing.FontStyle]::Bold)
$lblBackStatus.ForeColor = $SubTextColor
$lblBackStatus.TextAlign = [System.Drawing.ContentAlignment]::MiddleRight
$lblBackStatus.Location = New-Object System.Drawing.Point(270, 18)
$lblBackStatus.Size = New-Object System.Drawing.Size(185, 25)
$statusPanel.Controls.Add($lblBackStatus)

# Separator Line
$sepLine = New-Object System.Windows.Forms.Label
$sepLine.BorderStyle = [System.Windows.Forms.BorderStyle]::Fixed3D
$sepLine.Location = New-Object System.Drawing.Point(18, 58)
$sepLine.Size = New-Object System.Drawing.Size(440, 2)
$statusPanel.Controls.Add($sepLine)

# Frontend Row
$lblFrontName = New-Object System.Windows.Forms.Label
$lblFrontName.Text = "Frontend (React Vite)"
$lblFrontName.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$lblFrontName.ForeColor = $TextColor
$lblFrontName.Location = New-Object System.Drawing.Point(18, 66)
$lblFrontName.Size = New-Object System.Drawing.Size(220, 22)
$statusPanel.Controls.Add($lblFrontName)

$lblFrontPort = New-Object System.Windows.Forms.Label
$lblFrontPort.Text = "http://localhost:5173"
$lblFrontPort.Font = New-Object System.Drawing.Font("Segoe UI", 8.5)
$lblFrontPort.ForeColor = $SubTextColor
$lblFrontPort.Location = New-Object System.Drawing.Point(18, 88)
$lblFrontPort.Size = New-Object System.Drawing.Size(220, 18)
$statusPanel.Controls.Add($lblFrontPort)

$lblFrontStatus = New-Object System.Windows.Forms.Label
$lblFrontStatus.Text = "Verification..."
$lblFrontStatus.Font = New-Object System.Drawing.Font("Segoe UI", 9.5, [System.Drawing.FontStyle]::Bold)
$lblFrontStatus.ForeColor = $SubTextColor
$lblFrontStatus.TextAlign = [System.Drawing.ContentAlignment]::MiddleRight
$lblFrontStatus.Location = New-Object System.Drawing.Point(270, 68)
$lblFrontStatus.Size = New-Object System.Drawing.Size(185, 25)
$statusPanel.Controls.Add($lblFrontStatus)

# Main Launch Button
$btnStart = New-Object System.Windows.Forms.Button
$btnStart.Text = "Demarrer l'application (Back + Front)"
$btnStart.Font = New-Object System.Drawing.Font("Segoe UI", 11, [System.Drawing.FontStyle]::Bold)
$btnStart.BackColor = [System.Drawing.ColorTranslator]::FromHtml("#2E7D32")
$btnStart.ForeColor = [System.Drawing.Color]::White
$btnStart.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
$btnStart.FlatAppearance.BorderSize = 0
$btnStart.Location = New-Object System.Drawing.Point(24, 205)
$btnStart.Size = New-Object System.Drawing.Size(476, 46)
$btnStart.Cursor = [System.Windows.Forms.Cursors]::Hand
$form.Controls.Add($btnStart)

# Open Browser Button
$btnOpen = New-Object System.Windows.Forms.Button
$btnOpen.Text = "Ouvrir l'application dans le navigateur"
$btnOpen.Font = New-Object System.Drawing.Font("Segoe UI", 10.5, [System.Drawing.FontStyle]::Bold)
$btnOpen.BackColor = $AccentBtn
$btnOpen.ForeColor = [System.Drawing.Color]::White
$btnOpen.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
$btnOpen.FlatAppearance.BorderSize = 0
$btnOpen.Location = New-Object System.Drawing.Point(24, 260)
$btnOpen.Size = New-Object System.Drawing.Size(476, 44)
$btnOpen.Cursor = [System.Windows.Forms.Cursors]::Hand
$btnOpen.Enabled = $false
$form.Controls.Add($btnOpen)

# Stop Button
$btnStop = New-Object System.Windows.Forms.Button
$btnStop.Text = "Tout Arreter (Arreter Back & Front)"
$btnStop.Font = New-Object System.Drawing.Font("Segoe UI", 11, [System.Drawing.FontStyle]::Bold)
$btnStop.BackColor = $DangerBtn
$btnStop.ForeColor = [System.Drawing.Color]::White
$btnStop.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
$btnStop.FlatAppearance.BorderSize = 0
$btnStop.Location = New-Object System.Drawing.Point(24, 314)
$btnStop.Size = New-Object System.Drawing.Size(476, 46)
$btnStop.Cursor = [System.Windows.Forms.Cursors]::Hand
$btnStop.Enabled = $false
$form.Controls.Add($btnStop)

# Secondary Buttons Row: Swagger & Refresh
$btnSwagger = New-Object System.Windows.Forms.Button
$btnSwagger.Text = "API Swagger"
$btnSwagger.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$btnSwagger.BackColor = $SecondaryBtn
$btnSwagger.ForeColor = $TextColor
$btnSwagger.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
$btnSwagger.FlatAppearance.BorderSize = 0
$btnSwagger.Location = New-Object System.Drawing.Point(24, 372)
$btnSwagger.Size = New-Object System.Drawing.Size(232, 34)
$btnSwagger.Cursor = [System.Windows.Forms.Cursors]::Hand
$form.Controls.Add($btnSwagger)

$btnRefresh = New-Object System.Windows.Forms.Button
$btnRefresh.Text = "Actualiser l'etat"
$btnRefresh.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$btnRefresh.BackColor = $SecondaryBtn
$btnRefresh.ForeColor = $TextColor
$btnRefresh.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
$btnRefresh.FlatAppearance.BorderSize = 0
$btnRefresh.Location = New-Object System.Drawing.Point(268, 372)
$btnRefresh.Size = New-Object System.Drawing.Size(232, 34)
$btnRefresh.Cursor = [System.Windows.Forms.Cursors]::Hand
$form.Controls.Add($btnRefresh)

# Log / Info Label
$lblLog = New-Object System.Windows.Forms.Label
$lblLog.Text = "Initialisation..."
$lblLog.Font = New-Object System.Drawing.Font("Segoe UI", 8.5)
$lblLog.ForeColor = $SubTextColor
$lblLog.Location = New-Object System.Drawing.Point(24, 420)
$lblLog.Size = New-Object System.Drawing.Size(476, 40)
$form.Controls.Add($lblLog)

# --- Helper Functions ---
function Test-PortOnline([int]$port) {
    try {
        $tcp = New-Object System.Net.Sockets.TcpClient
        $ar = $tcp.BeginConnect("127.0.0.1", $port, $null, $null)
        $wait = $ar.AsyncWaitHandle.WaitOne(200, $false)
        if ($wait) {
            $tcp.EndConnect($ar)
            $tcp.Close()
            return $true
        }
        $tcp.Close()
        return $false
    } catch {
        return $false
    }
}

$ScriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }

function Run-Wsl([string]$scriptName) {
    $winPath = Join-Path $ScriptDir $scriptName
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = "wsl.exe"
    $psi.Arguments = "-e bash -lic ""`$(wslpath -a '$winPath')"""
    $psi.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
    $psi.CreateNoWindow = $true
    $psi.UseShellExecute = $false
    $proc = [System.Diagnostics.Process]::Start($psi)
    $proc.WaitForExit(15000)
}

function Update-State {
    $backOk = Test-PortOnline 5000
    $frontOk = Test-PortOnline 5173

    if ($backOk) {
        $lblBackStatus.Text = "[EN LIGNE] 5000"
        $lblBackStatus.ForeColor = $GreenColor
    } else {
        $lblBackStatus.Text = "[ARRETE]"
        $lblBackStatus.ForeColor = $RedColor
    }

    if ($frontOk) {
        $lblFrontStatus.Text = "[EN LIGNE] 5173"
        $lblFrontStatus.ForeColor = $GreenColor
    } else {
        $lblFrontStatus.Text = "[ARRETE]"
        $lblFrontStatus.ForeColor = $RedColor
    }

    if ($frontOk -and $backOk) {
        $btnStart.Text = "Application Active (Back + Front)"
        $btnStart.BackColor = [System.Drawing.ColorTranslator]::FromHtml("#1B4332")
        $btnStart.Enabled = $false
        $btnOpen.Enabled = $true
        $btnStop.Enabled = $true
        $lblLog.Text = "Tous les services sont en ligne et operationnels."
    } elseif ($frontOk -or $backOk) {
        $btnStart.Text = "Relancer les services manquants"
        $btnStart.BackColor = [System.Drawing.ColorTranslator]::FromHtml("#2E7D32")
        $btnStart.Enabled = $true
        $btnOpen.Enabled = $frontOk
        $btnStop.Enabled = $true
        $lblLog.Text = "Certains services sont actifs."
    } else {
        $btnStart.Text = "Demarrer l'application (Back + Front)"
        $btnStart.BackColor = [System.Drawing.ColorTranslator]::FromHtml("#2E7D32")
        $btnStart.Enabled = $true
        $btnOpen.Enabled = $false
        $btnStop.Enabled = $false
        $lblLog.Text = "Application arretee. Cliquez sur Demarrer pour lancer les services."
    }
}

# --- Event Handlers ---
$btnStart.Add_Click({
    $btnStart.Enabled = $false
    $lblLog.Text = "Lancement du Backend et Frontend via WSL en cours..."
    $lblLog.ForeColor = $BlueColor
    $form.Update()

    Run-Wsl "start_services.sh"

    Start-Sleep -Seconds 2
    Update-State
    $lblLog.ForeColor = $SubTextColor

    if (Test-PortOnline 5173) {
        Start-Process "http://localhost:5173"
    }
})

$btnStop.Add_Click({
    $btnStop.Enabled = $false
    $lblLog.Text = "Arret de tous les services en cours..."
    $lblLog.ForeColor = $DangerBtn
    $form.Update()

    Run-Wsl "stop_services.sh"

    Start-Sleep -Seconds 1
    Update-State
    $lblLog.ForeColor = $SubTextColor
})

$btnOpen.Add_Click({
    Start-Process "http://localhost:5173"
})

$btnSwagger.Add_Click({
    Start-Process "http://localhost:5000/swagger"
})

$btnRefresh.Add_Click({
    $lblLog.Text = "Actualisation..."
    Update-State
})

# Timer for periodic refresh
$timer = New-Object System.Windows.Forms.Timer
$timer.Interval = 2500
$timer.Add_Tick({
    Update-State
})
$timer.Start()

# Confirmation on Form Close
$form.Add_FormClosing({
    param($sender, $e)
    $timer.Stop()
    $backOk = Test-PortOnline 5000
    $frontOk = Test-PortOnline 5173

    if ($backOk -or $frontOk) {
        $result = [System.Windows.Forms.MessageBox]::Show(
            "Les services HiddenJobs Hunter sont encore actifs en arriere-plan.`n`nSouhaitez-vous les arreter avant de quitter ?",
            "HiddenJobs Hunter",
            [System.Windows.Forms.MessageBoxButtons]::YesNoCancel,
            [System.Windows.Forms.MessageBoxIcon]::Question
        )

        if ($result -eq [System.Windows.Forms.DialogResult]::Yes) {
            Run-Wsl "stop_services.sh"
        } elseif ($result -eq [System.Windows.Forms.DialogResult]::Cancel) {
            $e.Cancel = $true
            $timer.Start()
        }
    }
})

# Initial State
Update-State

# Show Form
[System.Windows.Forms.Application]::Run($form)
