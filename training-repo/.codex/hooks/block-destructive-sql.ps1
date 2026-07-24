$inputJson = [Console]::In.ReadToEnd()

if ($inputJson -match '(?i)\bDROP\s+TABLE\b|\bTRUNCATE(?:\s+TABLE)?\b') {
    [Console]::Error.WriteLine(
        'Destructive SQL is blocked by the OrderHub project hook. Use a migration or ask for explicit approval.'
    )
    exit 2
}

exit 0
