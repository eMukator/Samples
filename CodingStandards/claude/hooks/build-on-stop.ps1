# build-on-stop.ps1 — SubagentStop hook (registrovany s matcherem na vyvojare)
# Umisteni: ~/.claude/hooks/build-on-stop.ps1
#
# Po skonceni agenta spusti build projektu podle "buildOnStop" (pole: program + argumenty)
# v <projekt>/.claude/hooks-config.json. Projekt bez teto polozky se nebuildi.
# Build selze -> exit 2 a chyby na stderr: agent nesmi skoncit a dostane je k oprave.
# Druhe zastaveni (stop_hook_active) uz neblokuje.
# ponytail: jeden vynuceny pokus, zbytek chyti tester; vic kol pridat, kdyz se to ukaze malo.

$ErrorActionPreference = 'Stop'

try {
    $in = [Console]::In.ReadToEnd() | ConvertFrom-Json
} catch {
    exit 0
}

if ($in.stop_hook_active) { exit 0 }

$root = $env:CLAUDE_PROJECT_DIR
if (-not $root) { exit 0 }

$cfgPath = Join-Path $root '.claude/hooks-config.json'
if (-not (Test-Path $cfgPath)) { exit 0 }
try { $cfg = Get-Content $cfgPath -Raw -Encoding UTF8 | ConvertFrom-Json } catch { exit 0 }
if ($cfg.PSObject.Properties.Name -notcontains 'buildOnStop') { exit 0 }

$cmd = @($cfg.buildOnStop)
$exe = $cmd[0]
$cmdArgs = @($cmd | Select-Object -Skip 1)

$log = Join-Path $root '.scratch/build-on-stop.log'
New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null

Push-Location $root
try {
    & $exe @cmdArgs *> $log
    $code = $LASTEXITCODE
} finally {
    Pop-Location
}

if ($code -eq 0) { exit 0 }

$errors = Select-String -Path $log -Pattern ': error ' |
    ForEach-Object { $_.Line.Trim() } |
    Select-Object -Unique -First 30

[Console]::Error.WriteLine("Build po tve praci selhal (exit $code, log $log). Oprav chyby a pak teprve odevzdej hlaseni:")
foreach ($e in $errors) { [Console]::Error.WriteLine($e) }
exit 2
