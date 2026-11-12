"""Offline EN->FR worker. No network client, API key or telemetry."""
import json
import os
from pathlib import Path
import re
import sys

os.environ["HF_HUB_OFFLINE"] = "1"


class Translator:
    def __init__(self, root):
        import ctranslate2
        import sentencepiece
        self.source = sentencepiece.SentencePieceProcessor(model_file=str(root / "source.spm"))
        self.target = sentencepiece.SentencePieceProcessor(model_file=str(root / "target.spm"))
        self.engine = ctranslate2.Translator(str(root), device="cpu", compute_type="int8",
                                           inter_threads=1, intra_threads=2)

    def text(self, text):
        # Keep chat speaker labels and RP delimiters outside the translation model.
        prefix = ""
        match = re.match(r"^(/(?:me|do|ame)\s+|\[[^\]\r\n]{1,40}\]\s*)", text, re.I)
        if match:
            prefix, text = match.group(), text[match.end():]
        speaker = re.match(r"^([A-Z][\w'-]+(?: [A-Z][\w'-]+){0,3})(?: (says|shouts|whispers))?:\s*", text)
        if speaker:
            verbs = {"says": "dit", "shouts": "crie", "whispers": "chuchote"}
            prefix += speaker[1] + (" " + verbs[speaker[2]] if speaker[2] else "") + " : "
            text = text[speaker.end():]
        chunks = re.split(r"(\*+|\r?\n)", text)
        result = []
        for chunk in chunks:
            if not chunk.strip() or re.fullmatch(r"\*+|\r?\n", chunk):
                result.append(chunk)
                continue
            tokens = self.source.encode(chunk.strip(), out_type=str) + ["</s>"]
            if len(tokens) > 480:
                raise ValueError("Message trop long pour le traducteur local.")
            output = self.engine.translate_batch([tokens], beam_size=3, max_decoding_length=512)[0].hypotheses[0]
            translated = self.target.decode([t for t in output if t not in {"</s>", "<pad>"}])
            if not translated.strip():
                raise ValueError("Traduction locale vide.")
            left = chunk[:len(chunk)-len(chunk.lstrip())]
            right = chunk[len(chunk.rstrip()):]
            result.append(left + translated + right)
        return prefix + "".join(result)


def main():
    root = Path(sys.executable).parent if getattr(sys, "frozen", False) else Path(__file__).parent
    translator = None
    for raw in sys.stdin.buffer:
        try:
            request = json.loads(raw)
            lines = request["lines"]
            if not isinstance(lines, list) or not 1 <= len(lines) <= 6:
                raise ValueError("Lot local invalide.")
            if any(not isinstance(x.get("text"), str) or not x.get("id") or len(x["text"]) > 3000 for x in lines):
                raise ValueError("Message local invalide.")
            if translator is None:
                translator = Translator(root / "model")
            output = [{"id": x["id"], "text": translator.text(x["text"])} for x in lines]
            result = {"ok": True, "lines": output}
        except Exception as error:
            result = {"ok": False, "message": str(error)[:300]}
        sys.stdout.buffer.write((json.dumps(result, ensure_ascii=False) + "\n").encode("utf-8"))
        sys.stdout.buffer.flush()


if __name__ == "__main__":
    main()
