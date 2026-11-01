$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
$testDirectory = Join-Path $tempBase ('matric-demo-checks-' + [Guid]::NewGuid().ToString('N'))
try {
    New-Item -ItemType Directory -Path $testDirectory | Out-Null
    $compiler = & (Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe') -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\Roslyn\csc.exe' | Select-Object -First 1
    if (!$compiler) { throw 'Visual Studio C# compiler was not found.' }
    $json = Join-Path $repo 'packages/Newtonsoft.Json.13.0.4/lib/net45/Newtonsoft.Json.dll'
    $runner = Join-Path $testDirectory 'DemoChecks.exe'
    & $compiler /nologo /target:exe "/out:$runner" "/reference:$json" (Join-Path $repo 'DemoCommon.cs') (Join-Path $repo 'demos/raw/DemoIntegrationHelper.cs') (Join-Path $PSScriptRoot 'DemoChecks.cs')
    if ($LASTEXITCODE) { throw 'Demo checks compilation failed.' }
    Copy-Item -LiteralPath $json -Destination $testDirectory
    & $runner
    if ($LASTEXITCODE) { throw 'Demo checks failed.' }
} finally {
    $resolved = [IO.Path]::GetFullPath($testDirectory)
    if (!$resolved.StartsWith($tempBase, [StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($resolved) -notmatch '^matric-demo-checks-[a-f0-9]{32}$') { throw 'Unsafe test cleanup path.' }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
