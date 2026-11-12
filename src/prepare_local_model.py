"""Build-time download/conversion only; never included in the offline worker."""
import json
from pathlib import Path
import shutil
import sys
from huggingface_hub import snapshot_download
from ctranslate2.converters import TransformersConverter

model = "Helsinki-NLP/opus-mt-en-fr"
revision = "dd7f6540a7a48a7f4db59e5c0b9c42c8eea67f18"
source = Path(snapshot_download(model, revision=revision, local_dir=str(Path(sys.argv[1]).resolve().parent / "opus-source"),
    allow_patterns=["config.json", "pytorch_model.bin", "*.spm", "vocab.json", "tokenizer_config.json", "README.md"]))
destination = Path(sys.argv[1])
TransformersConverter(str(source)).convert(str(destination), quantization="int8", force=True)
for name in ("source.spm", "target.spm", "README.md"):
    shutil.copyfile(source / name, destination / name)
(destination / "provenance.json").write_text(json.dumps({"model": model, "revision": revision,
    "license": "Apache-2.0", "quantization": "int8", "device": "cpu"}, indent=2))
print("MODEL READY", revision)
