# guard-bash.ps1 — univerzalni PreToolUse hook pro nastroj Bash
# Umisteni: ~/.claude/hooks/guard-bash.ps1
#
# Vestavena pravidla resi VYHRADNE procesni hygienu na Windows:
# osirele skenery a procesy na pozadi, ktere Claude Code neuvolni.
# Nic o konkretnim jazyce ci frameworku zde natvrdo neni.
#
# Konfigurace ve trech vrstvach (pozdejsi prepisuje/doplnuje drivejsi):
#   1. vestavena pravidla nize
#   2. ~/.claude/hooks/hooks-config.json          (uzivatelske preference)
#   3. <projekt>/.claude/hooks-config.json        (projektova specifika)
#
# Exit kod je vzdy 0 — zamitnuti se predava JSONem, ne exit kodem.

$ErrorActionPreference = 'Stop'

try {
    $in = [Console]::In.ReadToEnd() | ConvertFrom-Json
} catch {
    exit 0
}

$cmd = $in.tool_input.command
if ([string]::IsNullOrWhiteSpace($cmd)) { exit 0 }

$bg = $false
if ($null -ne $in.tool_input.run_in_background) {
    $bg = [bool]$in.tool_input.run_in_background
}

function Deny([string]$msg) {
    @{
        hookSpecificOutput = @{
            hookEventName            = 'PreToolUse'
            permissionDecision       = 'deny'
            permissionDecisionReason = $msg
        }
    } | ConvertTo-Json -Depth 5 -Compress
    exit 0
}

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

# --- nacteni konfigurace -----------------------------------------
$userCfg = Read-Config (Join-Path $HOME '.claude/hooks/hooks-config.json')

$projCfg = $null
if (-not [string]::IsNullOrWhiteSpace($env:CLAUDE_PROJECT_DIR)) {
    $projCfg = Read-Config (Join-Path $env:CLAUDE_PROJECT_DIR '.claude/hooks-config.json')
}

function Merge-Array([string]$key) {
    $out = @()
    foreach ($c in @($userCfg, $projCfg)) {
        $v = Get-Prop $c $key
        if ($v) { $out += @($v) }
    }
    return $out
}

function Merge-Scalar([string]$key) {
    $v = Get-Prop $projCfg $key
    if ($null -ne $v) { return $v }
    return (Get-Prop $userCfg $key)
}

$disabled     = @(Merge-Array 'disableRules')
$allowList    = @(Merge-Array 'allowPatterns')
$extraRules   = @(Merge-Array 'denyCommands')
$extraLR      = @(Merge-Array 'longRunning')
$port         = Merge-Scalar 'port'

# --- vyjimky maji prednost pred vsim -----------------------------
foreach ($a in $allowList) {
    if ($cmd -match $a) { exit 0 }
}

$B = '(?<![\w.\-/\\])'
$E = '(?![\w.\-])'

function Rule-Enabled([string]$id) { return ($disabled -notcontains $id) }

# --- 1) dlouhobezici procesy MUSI bezet na pozadi ----------------
if (Rule-Enabled 'foreground-server') {

    $longRunning = @(
        'dotnet\s+run', 'dotnet\s+watch', 'dotnet\s+exec', 'dotnet\s+\S+\.dll',
        'npm\s+start', 'npm\s+run\s+dev', 'yarn\s+dev', 'pnpm\s+dev',
        'vite(?![\w.\-])', 'webpack\s+serve', 'ng\s+serve', 'next\s+dev', 'nodemon',
        'python\s+-m\s+http\.server', 'flask\s+run', 'uvicorn', 'gunicorn',
        'rails\s+s(erver)?', 'php\s+-S', 'go\s+run',
        'docker\s+compose\s+up(?!\s+(-d|--detach))', 'docker\s+run(?!.*\s-d\b)',
        'tail\s+-f', 'watch\s+-n'
    ) + $extraLR

    $lrPattern = "$B(" + ($longRunning -join '|') + ")"

    if (($cmd -match $lrPattern) -and (-not $bg)) {
        Deny 'Dlouhobezici proces (server, watch) nespoustej v popredi - zablokuje session az do timeoutu a pak zustane viset jako osirely proces. Zavolej stejny prikaz znovu s run_in_background=true, start over dotazem na endpoint (curl), a po dokonceni pouzij TaskStop.'
    }
}

# --- 2) zmena portu (jen kdyz je port v konfiguraci) -------------
if ($port -and (Rule-Enabled 'port-change')) {
    if ($cmd -match "${B}--(urls?|port)[\s=]+\S*?:?(\d{4,5})\b") {
        if ($Matches[2] -ne [string]$port) {
            Deny "Nemen port aplikace. Konflikt portu znamena, ze bezi stara instance - tu ukonci, neobchazej ji spustenim jinde. Port tohoto projektu je $port."
        }
    }
}

# --- 3) vestavena pravidla procesni hygieny ----------------------
$rules = @(
    @{ id = 'find-root'
       p  = "${B}find\s+/(\s|$)"
       m  = 'find od korene prochazi v Git Bash cely disk C:. Zakazano bez vyjimky. Umisteni souboru zjistuj nastroji projektu, ne skenovanim disku.' },

    @{ id = 'find'
       p  = "$B(find)$E"
       m  = 'Nepouzivej find - na Windows zustava viset jako osirely proces. Hledani souboru dela nastroj Glob.' },

    @{ id = 'grep'
       p  = "$B(grep|egrep|fgrep|ack|rg)$E"
       m  = 'Nepouzivej grep - na Windows zustava viset jako osirely proces. Hledani v obsahu dela nastroj Grep.' },

    @{ id = 'head-tail'
       p  = "(?-i)$B(head|tail)$E"   # case-sensitive: git ... HEAD neni prikaz head
       m  = 'Nepouzivej head/tail. Uzavrena roura na Windows nezabije upstream proces, ktery pak bezi dal. Vystup presmeruj do souboru a precti nastrojem Read s parametry offset a limit.' },

    @{ id = 'text-tools'
       p  = "$B(sed|awk|cut|tr|xargs)$E"
       m  = 'Transformaci textu napis jako skript do souboru a spust ho souborem, ne pres rouru v shellu.' },

    @{ id = 'background-ops'
       p  = "$B(nohup|disown)$E"
       m  = 'nohup/disown odpoji proces od shellu a Claude Code ho pak nema jak ukoncit. Pouzij Bash s run_in_background=true a nasledne TaskStop.' },

    @{ id = 'ampersand'
       p  = '(?<!&)&(?!&)\s*(\r?\n|$)'
       m  = 'Operator & pro beh na pozadi je zakazan - takovy proces Claude Code neuvolni. Pouzij Bash s run_in_background=true.' },

    @{ id = 'long-sleep'
       p  = "${B}sleep\s+(1[1-9]|[2-9]\d|\d{3,})$E"   # CLAUDE.md: zakazano az nad 10 s
       m  = 'Dlouhy sleep blokuje session. Dlouhobezici proces spust s run_in_background a jeho stav over dotazem na endpoint.' },

    @{ id = 'ls-recursive'
       p  = "${B}ls\s+-\w*R"
       m  = 'Rekurzivni vypis nedelej pres ls -R, pouzij nastroj Glob.' },

    @{ id = 'kill-broad'
       p  = "${B}(taskkill\s+/IM|pkill|killall)$E"
       m  = 'Plosne zabijeni podle jmena procesu muze sestrelit Claude Code nebo build infrastrukturu. Ukoncuj konkretni PID, nebo pouzij TaskStop pro shelly, ktere jsi sam spustil.' }
)

foreach ($r in $rules) {
    if ((Rule-Enabled $r.id) -and ($cmd -match $r.p)) { Deny $r.m }
}

# --- 4) uzivatelska a projektova pravidla ------------------------
foreach ($e in $extraRules) {
    $pat = Get-Prop $e 'pattern'
    $msg = Get-Prop $e 'message'
    if ($pat -and ($cmd -match $pat)) {
        if (-not $msg) { $msg = "Prikaz zamitnut konfiguraci: $pat" }
        Deny $msg
    }
}

exit 0
