$platforms = @("x86", "x64", "arm64")

foreach ($plat in $platforms) {
    $rid = "win10-$plat"
    Write-Host "Publishing for $rid..."
    # 使用 -p:Platform=$plat 确保正确选择平台配置
    # 使用 -p:WindowsPackageType=None 确保生成非打包应用（如果 csproj 中已有条件判断则可选，但显式指定更安全）
    dotnet publish -c Release -r $rid -p:Platform=$plat -o ".\publish\$rid"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Publish failed for $rid"
        exit $LASTEXITCODE
    }
}

Write-Host "All platforms published successfully to .\publish\"
