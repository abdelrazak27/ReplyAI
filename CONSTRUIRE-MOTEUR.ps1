$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try {
 if(!(Test-Path .build-env\Scripts\python.exe)){python -m venv .build-env; if($LASTEXITCODE -ne 0){throw 'Python 3.13 et venv sont requis pour reconstruire le moteur.'}}
 & .\.build-env\Scripts\python.exe -m pip install -r requirements-engine.txt
 if($LASTEXITCODE -ne 0){throw 'Installation des dépendances impossible.'}
 & .\.build-env\Scripts\python.exe -m unittest discover -s tests -p test_python_engine.py
 if($LASTEXITCODE -ne 0){throw 'Les tests du moteur ont échoué.'}
 & .\.build-env\Scripts\python.exe -m PyInstaller --noconfirm --clean --onedir --console --name parole-engine --distpath .build-dist --workpath .build-work --specpath .build-work --exclude-module google --collect-all groq --copy-metadata groq --copy-metadata pydantic src\python_engine.py
 if($LASTEXITCODE -ne 0){throw 'Compilation du moteur impossible.'}
 New-Item -ItemType Directory -Force engine-v1.9 | Out-Null
 Copy-Item .build-dist\parole-engine\* engine-v1.9 -Recurse -Force
 Write-Output 'Moteur Python reconstruit dans engine-v1.9.'
} finally {Pop-Location}
