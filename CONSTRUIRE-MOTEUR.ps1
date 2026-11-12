$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try {
 if(!(Test-Path .build-env\Scripts\python.exe)){python -m venv .build-env; if($LASTEXITCODE -ne 0){throw 'Python 3.13 et venv sont requis pour reconstruire le moteur.'}}
 & .\.build-env\Scripts\python.exe -m pip install -r requirements-engine.txt
 if($LASTEXITCODE -ne 0){throw 'Installation des dépendances impossible.'}
 & .\.build-env\Scripts\python.exe -m unittest discover -s tests -p test_python_engine.py
 if($LASTEXITCODE -ne 0){throw 'Les tests du moteur ont échoué.'}
 & .\.build-env\Scripts\python.exe -m unittest discover -s tests -p test_translation.py
 if($LASTEXITCODE -ne 0){throw 'Les tests de traduction ont échoué.'}
 & .\.build-env\Scripts\python.exe -m PyInstaller --noconfirm --clean --onedir --console --name parole-engine --distpath .build-dist --workpath .build-work --specpath .build-work --exclude-module google --collect-all groq --copy-metadata groq --copy-metadata pydantic src\python_engine.py
 if($LASTEXITCODE -ne 0){throw 'Compilation du moteur impossible.'}
 New-Item -ItemType Directory -Force engine-v1.9 | Out-Null
 Copy-Item .build-dist\parole-engine\* engine-v1.9 -Recurse -Force
 Write-Output 'Moteur Python reconstruit dans engine-v1.9.'

 $translationPython = '.build-translation-env\Scripts\python.exe'
 if(!(Test-Path $translationPython)){
  python -m venv .build-translation-env
  if($LASTEXITCODE -ne 0){throw "Création de l’environnement de traduction impossible."}
 }
 & $translationPython -m pip install torch==2.15.0 --index-url https://download.pytorch.org/whl/cpu
 if($LASTEXITCODE -ne 0){throw 'Installation de PyTorch CPU impossible.'}
 & $translationPython -m pip install -r requirements-local-translation.txt
 if($LASTEXITCODE -ne 0){throw 'Installation des dépendances de traduction impossible.'}
 & ./CONSTRUIRE-TRADUCTEUR.ps1 -Python $translationPython -Destination .build-local-dist
 $translatorPack = '.build-local-dist\reply-translate'
 & $translationPython -X utf8 tests/test_local_translation.py "$translatorPack\reply-translate.exe"
 if($LASTEXITCODE -ne 0){throw 'Le traducteur hors ligne a échoué au test.'}
 New-Item -ItemType Directory -Force engine-local | Out-Null
 Copy-Item "$translatorPack\*" engine-local -Recurse -Force
} finally {Pop-Location}
