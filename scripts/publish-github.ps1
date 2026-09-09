# Creates public GitHub repo and pushes coding project (clone without password).
# Prerequisite: gh auth login

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

$Owner = "amirreza-fnt"
$Repo = "coding-system"

gh auth status | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Run first: gh auth login" -ForegroundColor Yellow
    exit 1
}

if (git remote get-url origin 2>$null) {
    Write-Host "Remote origin already exists. Pushing only..."
    git push -u origin main
    exit $LASTEXITCODE
}

gh repo create "$Owner/$Repo" `
    --public `
    --source . `
    --remote origin `
    --push `
    --description "Centralized 5-digit tracking code service (Sabzevar Man)"

Write-Host ""
Write-Host "Done: https://github.com/$Owner/$Repo" -ForegroundColor Green
Write-Host "Clone: git clone https://github.com/$Owner/$Repo.git"
