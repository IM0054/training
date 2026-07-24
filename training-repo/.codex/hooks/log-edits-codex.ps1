$rawInput = [Console]::In.ReadToEnd()

try {
    $payload = $rawInput | ConvertFrom-Json
}
catch {
    exit 0
}

$paths = [System.Collections.Generic.List[string]]::new()
$command = [string]$payload.tool_input.command

if ($command) {
    foreach ($match in [regex]::Matches(
        $command,
        '(?m)^\*\*\* (?:Add|Update|Delete) File: (.+)$'
    )) {
        $paths.Add($match.Groups[1].Value.Trim())
    }
}

if ($paths.Count -eq 0 -and $payload.tool_input.file_path) {
    $paths.Add([string]$payload.tool_input.file_path)
}

if ($paths.Count -eq 0) {
    exit 0
}

$logPath = Join-Path $PSScriptRoot 'edit-log.txt'
$timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'

foreach ($path in $paths) {
    $line = '{0}  {1}  {2}' -f $timestamp, $payload.tool_name, $path
    Add-Content -LiteralPath $logPath -Value $line -Encoding UTF8
}

@{
    systemMessage = "OrderHub 編輯 hook 已記錄 $($paths.Count) 個異動檔案。"
} | ConvertTo-Json -Compress
