# Script para preparar deploy do Elastic Beanstalk
Write-Host "=== PREPARANDO DEPLOY ELASTIC BEANSTALK ===" -ForegroundColor Green

# 1. Limpando diretórios temporários
Write-Host "1. Limpando diretórios temporários..." -ForegroundColor Yellow
if (Test-Path "eb-deploy-bundle") { Remove-Item "eb-deploy-bundle" -Recurse -Force }
if (Test-Path "publish") { Remove-Item "publish" -Recurse -Force }
if (Test-Path "eb-deploy-bundle.zip") { Remove-Item "eb-deploy-bundle.zip" -Force }

# 2. Build da aplicação
Write-Host "2. Fazendo build da aplicação..." -ForegroundColor Yellow
dotnet build -c Release

# 3. Publicando aplicação
Write-Host "3. Publicando aplicação..." -ForegroundColor Yellow
dotnet publish -c Release -o publish

# 4. Criando bundle de deploy
Write-Host "4. Criando bundle de deploy..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path "eb-deploy-bundle" -Force | Out-Null

# 5. Copiando arquivos publicados
Write-Host "5. Copiando arquivos..." -ForegroundColor Yellow
Copy-Item "publish\*" -Destination "eb-deploy-bundle\" -Recurse -Force

# 6. Copiando configurações
Write-Host "6. Copiando configurações..." -ForegroundColor Yellow
if (Test-Path ".ebextensions") {
    Copy-Item ".ebextensions" -Destination "eb-deploy-bundle\" -Recurse -Force
}

# 7. Criando Procfile para Linux
Write-Host "7. Criando Procfile..." -ForegroundColor Yellow
$procfileContent = "web: dotnet TechChallenge.dll"
$procfileContent | Out-File -FilePath "eb-deploy-bundle\Procfile" -Encoding UTF8 -NoNewline

# 8. Criando arquivo ZIP
Write-Host "8. Criando arquivo ZIP..." -ForegroundColor Yellow
Compress-Archive -Path "eb-deploy-bundle\*" -DestinationPath "eb-deploy-bundle.zip" -Force

# 9. Limpando arquivos temporários
Write-Host "9. Limpando arquivos temporários..." -ForegroundColor Yellow
Remove-Item "eb-deploy-bundle" -Recurse -Force
Remove-Item "publish" -Recurse -Force

Write-Host "`n=== DEPLOY PREPARADO COM SUCESSO! ===" -ForegroundColor Green
Write-Host "📦 Arquivo: eb-deploy-bundle.zip" -ForegroundColor Cyan
$zipSize = (Get-Item "eb-deploy-bundle.zip").Length / 1MB
Write-Host "📁 Tamanho: $([math]::Round($zipSize, 2)) MB" -ForegroundColor Cyan

Write-Host "`n🎯 Próximos passos:" -ForegroundColor Yellow
Write-Host "1. Faça upload do arquivo 'eb-deploy-bundle.zip' no console da AWS Elastic Beanstalk" -ForegroundColor White
Write-Host "2. Configure as variáveis de ambiente AWS no console" -ForegroundColor White
Write-Host "3. Aguarde o deploy completar" -ForegroundColor White 