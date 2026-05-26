# 将旧版 config.json 迁移为 HoverTranslate MVP 格式（保留原文件备份）
$ErrorActionPreference = "Stop"
$configDir = Join-Path $env:USERPROFILE ".hover-translate"
$configPath = Join-Path $configDir "config.json"
$examplePath = Join-Path $PSScriptRoot "..\config.example.json"

if (-not (Test-Path $configDir)) {
    New-Item -ItemType Directory -Path $configDir -Force | Out-Null
}

if (-not (Test-Path $configPath)) {
    Copy-Item $examplePath $configPath
    Write-Host "已创建默认 config.json"
    exit 0
}

$raw = Get-Content $configPath -Raw -Encoding UTF8
if ($raw -match '"provider"\s*:') {
    Write-Host "config.json 已是新格式，跳过迁移。"
    exit 0
}

$old = $raw | ConvertFrom-Json
$backupPath = Join-Path $configDir ("config.json.bak-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
Copy-Item $configPath $backupPath -Force

$model = "qwen-turbo"
if ($old.model_chain -and $old.model_chain.Count -gt 0) {
    $idx = if ($null -ne $old.current_model_index) { [int]$old.current_model_index } else { 0 }
    if ($idx -ge 0 -and $idx -lt $old.model_chain.Count) { $model = [string]$old.model_chain[$idx] }
}

$newJson = @{
    provider = "bailian"
    hotkey = "Ctrl+Shift+T"
    enableHistory = [bool]$old.enable_history
    sanitize = if ($null -ne $old.sanitize) { [bool]$old.sanitize } else { $true }
    maxCharsPerRequest = 2000
    deepseek = @{
        apiKey = ""
        baseUrl = "https://api.deepseek.com"
        model = "deepseek-chat"
    }
    bailian = @{
        apiKey = [string]$old.api_key
        baseUrl = if ($old.base_url) { [string]$old.base_url } else { "https://dashscope.aliyuncs.com/compatible-mode/v1" }
        model = $model
    }
    glossary = @{
        Agent = "智能体"
        Composer = "Composer"
        Glass = "Glass"
        "User Rules" = "用户规则"
    }
} | ConvertTo-Json -Depth 5

Set-Content $configPath $newJson -Encoding UTF8
Write-Host "已迁移配置，备份: $backupPath"
