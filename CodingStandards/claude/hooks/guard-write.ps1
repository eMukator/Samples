# guard-write.ps1 — PreToolUse hook pro Write/Edit/NotebookEdit
# Umisteni: ~/.claude/hooks/guard-write.ps1
#
# Zamitne zapis do chranenych cest. Pravidla jsou v "protectedPaths"
# (pattern = regex nad cestou s '/', message = duvod a spravna nahrada)
# v ~/.claude/hooks/hooks-config.json a <projekt>/.claude/hooks-config.json.
# Exit kod je vzdy 0 — zamitnuti se predava JSONem.

$ErrorActionPreference = 'Stop'

try {
    $in = [Console]::In.ReadToEnd() | ConvertFrom-Json
} catch {
    exit 0
}

$path = $in.tool_input.file_path
if (-not $path) { $path = $in.tool_input.notebook_path }
if ([string]::IsNullOrWhiteSpace($path)) { exit 0 }
$path = $path -replace '\\', '/'

$configs = @(Join-Path $HOME '.claude/hooks/hooks-config.json')
if ($env:CLAUDE_PROJECT_DIR) { $configs += Join-Path $env:CLAUDE_PROJECT_DIR '.claude/hooks-config.json' }

foreach ($c in $configs) {
    if (-not (Test-Path $c)) { continue }
    try { $cfg = Get-Content $c -Raw -Encoding UTF8 | ConvertFrom-Json } catch { continue }
    if ($cfg.PSObject.Properties.Name -notcontains 'protectedPaths') { continue }

    foreach ($r in @($cfg.protectedPaths)) {
        if ($r.pattern -and ($path -match $r.pattern)) {
            $msg = if ($r.message) { $r.message } else { "Chranena cesta ($($r.pattern))." }
            @{
                hookSpecificOutput = @{
                    hookEventName            = 'PreToolUse'
                    permissionDecision       = 'deny'
                    permissionDecisionReason = $msg
                }
            } | ConvertTo-Json -Depth 5 -Compress
            exit 0
        }
    }
}

exit 0
