$ErrorActionPreference='Stop';$out="$env:TEMP\mt5setup.exe";Invoke-WebRequest 'https://download.terminal.free/cdn/web/metaquotes.ltd/mt5/mt5setup.exe' -OutFile $out;Start-Process $out -Wait
