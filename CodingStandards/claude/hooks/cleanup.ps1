# cleanup.ps1 — univerzalni SubagentStop / Stop hook
# Umisteni: ~/.claude/hooks/cleanup.ps1
#
# ZASADA: radeji nechat bezet neco, co melo umrit, nez zabit neco,
# co bezet melo. Hook se spousti po kazdem tahu agenta (klidne
# kazdych par desitek sekund), takze nesmi sahat na procesy, ktere
# prave ted nekdo legitimne pouziva.
#
# Zabiji se pouze:
#   1) osirele shellove skenery (find/grep/...) starsi nez 2 min
#   2) proces, jehoz EXE lezi primo v adresari projektu (typicky bin/)
#   3) znamy runtime spusteny nad artefaktem uvnitr projektu
#      (napr. dotnet exec <root>/bin/App.dll)
#
#   ... a u bodu 2 a 3 navic jen tehdy, bezi-li dele nez
#   projectMinAgeMinutes (vychozi 15). Legitimni overovani serveru
#   trva jednotky minut; cokoli starsiho uz je zapomenuty proces.
#
# Nikdy se nezabiji:
#   - predci tohoto procesu (Claude Code teto session)
#   - procesy z ochranneho seznamu (IDE, editory, shelly, Claude Code)
#   - deti IDE a debuggeru (aplikace ladena z Visual Studia, Rideru, VS Code)
#   - build infrastruktura
#   - procesy mimo adresar projektu
#
# Rezim nacisto: "cleanup": { "dryRun": true } v hooks-config.json

$ErrorActionPreference = 'SilentlyContinue'

function Get-Prop($obj, [string]$name) {
    if ($null -eq $obj) { return $null }
    if ($obj.PSObject.Properties.Name -contains $name) { return $obj.$name }
    return $null
}

function Read-Config([string]$path) {
    if ([string]::IsNullOrWhiteSpace($path)) { return $null }
    if (-not (Test-Path $path)) { return $null }
    try { return (Get-Content $path -Raw -Encoding UTF8 | ConvertFrom-Json) } catch { return $null }
}

$root    = $env:CLAUDE_PROJECT_DIR
$userCfg = Read-Config (Join-Path $HOME '.claude/hooks/hooks-config.json')
$projCfg = $null
if (-not [string]::IsNullOrWhiteSpace($root)) {
    $projCfg = Read-Config (Join-Path $root '.claude/hooks-config.json')
}
$cU = Get-Prop $userCfg 'cleanup'
$cP = Get-Prop $projCfg 'cleanup'

function Cfg([string]$key, $default) {
    $v = Get-Prop $cP $key
    if ($null -ne $v) { return $v }
    $v = Get-Prop $cU $key
    if ($null -ne $v) { return $v }
    return $default
}

function CfgArray([string]$key, [array]$default) {
    $out = @()
    foreach ($c in @($cU, $cP)) {
        $v = Get-Prop $c $key
        if ($v) { $out += @($v) }
    }
    return ($default + $out)
}

$dryRun       = [bool](Cfg 'dryRun' $false)
$scanMinAge   = [int](Cfg 'scannerMaxAgeMinutes' 2)
$projMinAge   = [int](Cfg 'projectMinAgeMinutes' 15)

# --- ochranny seznam podle jmena procesu -------------------------
$protectedNames = CfgArray 'protectNames' @(
    'devenv', 'ServiceHub', 'PerfWatson2', 'Microsoft', 'MSBuild', 'VBCSCompiler',
    'Code', 'code', 'rider64', 'idea64', 'studio64', 'sublime_text', 'notepad',
    'claude', 'cursor', 'windsurf',
    'pwsh', 'powershell', 'bash', 'sh', 'cmd', 'conhost', 'WindowsTerminal',
    'explorer', 'git', 'ssh', 'sqlservr', 'sqlwriter', 'ssms',
    'docker', 'com.docker.backend', 'wslhost', 'vmmem'
)
$protectRx = '^(' + (($protectedNames | ForEach-Object { [regex]::Escape($_) }) -join '|') + ')'

# --- ochranny seznam podle command line --------------------------
$protectCmdRx = CfgArray 'protectCommandPatterns' @(
    'claude', 'ServiceHub', 'VisualStudio', 'devenv',
    'MSBuild\.dll', 'VBCSCompiler', 'Roslyn',
    '\bdotnet(\.exe)?"?\s+(build|restore|test|list|nuget|ef|script|tool|msbuild|new|sln|pack|publish)\b',
    'esbuild', 'tsserver', 'jest-worker', 'language-server'
)
$protectCmd = '(' + ($protectCmdRx -join '|') + ')'

# --- predci tohoto procesu ---------------------------------------
$protectedPids = New-Object System.Collections.Generic.HashSet[int]
$cur = $PID
for ($i = 0; $i -lt 30; $i++) {
    if (-not $protectedPids.Add($cur)) { break }
    $p = Get-CimInstance Win32_Process -Filter "ProcessId=$cur"
    if (-not $p -or $p.ParentProcessId -le 0) { break }
    $cur = [int]$p.ParentProcessId
}

# --- rodice, jejichz deti se nezabiji (ladena aplikace z IDE) ------
$ideParents = CfgArray 'protectParentNames' @(
    'devenv', 'VsDebugConsole', 'msvsmon', 'rider64', 'Code', 'vsdbg', 'vsdbg-ui', 'idea64'
)
$ideParentRx = '^(' + (($ideParents | ForEach-Object { [regex]::Escape($_) }) -join '|') + ')\.exe$'

$killed = @()

function Try-Kill($proc, [string]$reason) {
    if ($protectedPids.Contains([int]$proc.ProcessId)) { return }
    if ($proc.Name -match $protectRx) { return }
    if ($proc.CommandLine    -and ($proc.CommandLine    -match $protectCmd)) { return }
    if ($proc.ExecutablePath -and ($proc.ExecutablePath -match $protectCmd)) { return }
    $parent = Get-CimInstance Win32_Process -Filter "ProcessId=$($proc.ParentProcessId)"
    if ($parent -and ($parent.Name -match $ideParentRx)) { return }

    $ageMin = [int]((Get-Date) - $proc.CreationDate).TotalMinutes

    $desc = $proc.ExecutablePath
    if (-not $desc) { $desc = $proc.CommandLine }
    if ($desc -and $desc.Length -gt 160) { $desc = $desc.Substring(0, 160) + '...' }

    $prefix = 'killed'
    if ($dryRun) { $prefix = 'DRY-RUN would kill' }

    $script:killed += "$prefix  [$reason] $($proc.Name) PID=$($proc.ProcessId) age=${ageMin}m :: $desc"

    if (-not $dryRun) { Stop-Process -Id $proc.ProcessId -Force }
}

# --- 1) osirele shellove skenery (globalne) ----------------------
$scanners = CfgArray 'scannerNames' @('find', 'grep', 'egrep', 'fgrep', 'sed', 'awk')
$scanRx   = '^(' + ($scanners -join '|') + ')\.exe$'

Get-CimInstance Win32_Process |
    Where-Object {
        $_.Name -match $scanRx -and
        $_.CreationDate -lt (Get-Date).AddMinutes(-1 * $scanMinAge)
    } |
    ForEach-Object { Try-Kill $_ 'orphan-scanner' }

# --- bez znameho projektu koncime --------------------------------
if ([string]::IsNullOrWhiteSpace($root) -or (-not (Test-Path $root))) { exit 0 }

$logDir = Join-Path $root '.scratch'
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$log   = Join-Path $logDir 'cleanup.log'
$stamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'

$rootNorm  = ($root -replace '\\', '/').TrimEnd('/').ToLowerInvariant()
$projCutoff = (Get-Date).AddMinutes(-1 * $projMinAge)

# --- 2) EXE lezici primo v adresari projektu ---------------------
Get-CimInstance Win32_Process |
    Where-Object {
        $_.ExecutablePath -and
        (($_.ExecutablePath -replace '\\', '/').ToLowerInvariant()).StartsWith("$rootNorm/") -and
        $_.CreationDate -lt $projCutoff
    } |
    ForEach-Object { Try-Kill $_ 'project-binary' }

# --- 3) znamy runtime nad artefaktem uvnitr projektu -------------
$runtimeRx  = '^(dotnet|node|python|python3|java|ruby|php|go)\.exe$'
$artifactRx = [regex]::Escape($rootNorm) + '[^\s"]*\.(dll|exe|js|mjs|cjs|py|jar|rb|php)\b'

Get-CimInstance Win32_Process |
    Where-Object {
        $_.Name -match $runtimeRx -and
        $_.CommandLine -and
        ((($_.CommandLine -replace '\\', '/').ToLowerInvariant()) -match $artifactRx) -and
        $_.CreationDate -lt $projCutoff
    } |
    ForEach-Object { Try-Kill $_ 'project-runtime' }

# --- zapis logu --------------------------------------------------
foreach ($k in $killed) {
    "$stamp  $k" | Add-Content $log -Encoding UTF8
}
