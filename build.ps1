$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$out = Join-Path $root "bin\taskbarpick.exe"

New-Item -ItemType Directory -Force (Join-Path $root "bin") | Out-Null
$sources = Get-ChildItem (Join-Path $root "src\*.cs") | ForEach-Object { $_.FullName }

& $csc /nologo /target:winexe /platform:x64 /optimize+ /out:$out `
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    $sources

if ($LASTEXITCODE -ne 0) { throw "build failed" }
"built $out"
