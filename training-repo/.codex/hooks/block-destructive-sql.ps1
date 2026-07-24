$inputJson = [Console]::In.ReadToEnd()

if ($inputJson -match '(?i)\bDROP\s+TABLE\b|\bTRUNCATE(?:\s+TABLE)?\b') {
    [Console]::Error.WriteLine(
        'OrderHub 專案 hook 已阻擋破壞性 SQL。請使用 Migration，或先取得明確核准。'
    )
    exit 2
}

exit 0
